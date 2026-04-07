using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using ProjectK._3PDB.Standalone.BL.Interfaces;
using ProjectK._3PDB.Standalone.BL.Models;
using ProjectK._3PDB.Standalone.Infrastructure.Context;
using ProjectK._3PDB.Standalone.Infrastructure.CsvMaps;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProjectK._3PDB.Standalone.BL.Services
{
    public class ParticipantService : IParticipantService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public ParticipantService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task ImportCsvAsync(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                MissingFieldFound = null,
                HeaderValidated = null,
            };

            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<ParticipantCsvMap>();

            var records = csv.GetRecords<ParticipantDto>().ToList();

            var entities = _mapper.Map<List<Participant>>(records);

            foreach (var record in entities)
            {
                record.History.Add(new Infrastructure.Entities.ParticipantHistory
                {
                    PropertyName = "Record",
                    OldValue = null,
                    NewValue = "Created via CSV Import",
                    ChangedAt = DateTime.Now,
                });
            }

            await _context.Participants.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var entities = await _context.Participants
                .Where(p => !p.IsDeleted)
                .OrderBy(x => x.Kurin)
                .ThenBy(x => x.FullName)
                .ToListAsync();

            var dtos = _mapper.Map<List<ParticipantDto>>(entities);

            using var memoryStream = new MemoryStream();
            using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);

            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            csvWriter.Context.RegisterClassMap<ParticipantCsvMap>();
            csvWriter.WriteRecords(dtos);

            streamWriter.Flush();

            return memoryStream.ToArray();
        }

        public async Task UpdateAsync(ParticipantDto dto)
        {
            var existingEntity = await _context.Participants
                .Include(p => p.History)
                .FirstOrDefaultAsync(p => p.ParticipantKey == dto.ParticipantKey && !p.IsDeleted);

            if (existingEntity == null) throw new Exception("Not found");

            var changes = DetectChanges(existingEntity, dto);

            _mapper.Map(dto, existingEntity);

            if (changes.Any())
            {
                foreach (var change in changes)
                {
                    existingEntity.History.Add(change);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ParticipantDto>> GetAllAsync()
        {
            var entities = await _context.Participants
                        .Where(p => !p.IsDeleted)
                        .AsNoTracking()
                        .ToListAsync();

            return _mapper.Map<List<ParticipantDto>>(entities);
        }

        public async Task<ParticipantDto?> GetByKeyAsync(Guid participantKey)
        {
            var entity = await _context.Participants
                        .Include(p => p.History)
                        .FirstOrDefaultAsync(p => p.ParticipantKey == participantKey && !p.IsDeleted);

            return _mapper.Map<ParticipantDto>(entity);
        }

        public async Task<List<ParticipantHistory>> GetHistoryAsync(Guid participantKey)
        {
            var participantExists = await _context.Participants
                .AsNoTracking()
                .AnyAsync(p => p.ParticipantKey == participantKey && !p.IsDeleted);

            if (!participantExists)
            {
                return [];
            }

            return await _context.ParticipantHistories
                .Where(h => h.ParticipantKey == participantKey && !h.IsDeleted)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task SoftDeleteHistoryAsync(Guid participantHistoryKey)
        {
            var history = await _context.ParticipantHistories
                .FirstOrDefaultAsync(h => h.ParticipantHistoryKey == participantHistoryKey && !h.IsDeleted);

            if (history == null)
            {
                throw new Exception("Not found");
            }

            history.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        public async Task<List<QuarterlyProbeReportItemDto>> GetQuarterlyProbeReportAsync(int year, int quarter)
        {
            if (quarter < 1 || quarter > 4)
            {
                throw new ArgumentException("Quarter must be between 1 and 4");
            }

            var quarterStartMonth = ((quarter - 1) * 3) + 1;
            var periodStart = new DateTime(year, quarterStartMonth, 1);
            var periodEnd = periodStart.AddMonths(3);

            var probeOpenRows = await _context.Participants
                .AsNoTracking()
                .Where(participant => !participant.IsDeleted
                                      && participant.ProbeOpenDate.HasValue
                                      && participant.ProbeOpenDate.Value >= periodStart
                                      && participant.ProbeOpenDate.Value < periodEnd)
                .Select(participant => new QuarterlyProbeReportItemDto
                {
                    ParticipantHistoryKey = null,
                    ParticipantKey = participant.ParticipantKey,
                    FullName = participant.FullName,
                    Kurin = participant.Kurin,
                    ProbeOpenDate = participant.ProbeOpenDate,
                    ChangedAt = participant.ProbeOpenDate!.Value,
                    Action = "opened",
                    OldValue = null,
                    NewValue = null
                })
                .ToListAsync();

            var archiveHistoryRows = await _context.ParticipantHistories
                .AsNoTracking()
                .Where(history => !history.IsDeleted
                                  && history.PropertyName == nameof(ParticipantDto.IsArchived)
                                  && history.ChangedAt >= periodStart
                                  && history.ChangedAt < periodEnd)
                .Join(
                    _context.Participants
                        .AsNoTracking()
                        .Where(participant => !participant.IsDeleted),
                    history => history.ParticipantKey,
                    participant => participant.ParticipantKey,
                    (history, participant) => new { history, participant })
                .ToListAsync();

            var archiveRows = archiveHistoryRows
                .Select(row => new QuarterlyProbeReportItemDto
                {
                    ParticipantHistoryKey = row.history.ParticipantHistoryKey,
                    ParticipantKey = row.history.ParticipantKey,
                    FullName = row.participant.FullName,
                    Kurin = row.participant.Kurin,
                    ProbeOpenDate = row.participant.ProbeOpenDate,
                    ChangedAt = row.history.ChangedAt,
                    Action = ResolveArchiveAction(row.history.OldValue, row.history.NewValue),
                    OldValue = LocalizeBooleanValue(row.history.OldValue),
                    NewValue = LocalizeBooleanValue(row.history.NewValue)
                })
                .Where(reportItem => !string.IsNullOrWhiteSpace(reportItem.Action))
                .ToList();

            var reportRows = probeOpenRows
                .Concat(archiveRows)
                .OrderByDescending(reportItem => reportItem.ChangedAt)
                .ToList();

            return reportRows;
        }

        public async Task<List<QuarterlyProbeTotalsItemDto>> GetQuarterlyProbeTotalsAsync(int year)
        {
            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            var probeOpenDates = await _context.Participants
                .AsNoTracking()
                .Where(participant => !participant.IsDeleted
                                      && participant.ProbeOpenDate.HasValue
                                      && participant.ProbeOpenDate.Value < yearEnd)
                .Select(participant => participant.ProbeOpenDate!.Value)
                .ToListAsync();

            var archiveHistoryRows = await _context.ParticipantHistories
                .AsNoTracking()
                .Where(history => !history.IsDeleted
                                  && history.PropertyName == nameof(ParticipantDto.IsArchived)
                                  && history.ChangedAt < yearEnd)
                .Join(
                    _context.Participants
                        .AsNoTracking()
                        .Where(participant => !participant.IsDeleted),
                    history => history.ParticipantKey,
                    participant => participant.ParticipantKey,
                    (history, participant) => new
                    {
                        history.ChangedAt,
                        history.OldValue,
                        history.NewValue
                    })
                .ToListAsync();

            var archivedDates = archiveHistoryRows
                .Where(row => ResolveArchiveAction(row.OldValue, row.NewValue) == "archived")
                .Select(row => row.ChangedAt)
                .ToList();

            var totals = new List<QuarterlyProbeTotalsItemDto>(capacity: 4);

            for (var quarter = 1; quarter <= 4; quarter += 1)
            {
                var quarterStart = new DateTime(year, ((quarter - 1) * 3) + 1, 1);
                var quarterEnd = quarterStart.AddMonths(3);

                var openedInQuarter = probeOpenDates.Count(date => date >= quarterStart && date < quarterEnd);
                var archivedInQuarter = archivedDates.Count(date => date >= quarterStart && date < quarterEnd);

                var openedUntilQuarterEnd = probeOpenDates.Count(date => date < quarterEnd);
                var archivedUntilQuarterEnd = archivedDates.Count(date => date < quarterEnd);

                totals.Add(new QuarterlyProbeTotalsItemDto
                {
                    Quarter = quarter,
                    OpenedTotal = Math.Max(0, openedUntilQuarterEnd - archivedUntilQuarterEnd),
                    OpenedInQuarter = openedInQuarter,
                    ArchivedInQuarter = archivedInQuarter
                });
            }

            return totals;
        }

        public async Task DeleteAsync (Guid participantKey)
        {
            var entity = await _context.Participants
                        .Include(p => p.History)
                        .FirstOrDefaultAsync(p => p.ParticipantKey == participantKey && !p.IsDeleted);
            if (entity == null) throw new Exception("Not found");

            entity.IsDeleted = true;
            entity.History.Add(new ParticipantHistory
            {
                PropertyName = "Record",
                OldValue = null,
                NewValue = "Soft deleted",
                ChangedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task<ParticipantDto> CreateAsync(ParticipantDto dto)
        {
            var entity = _mapper.Map<Participant>(dto);

            entity.History.Add(new ParticipantHistory
            {
                PropertyName = "Record",
                NewValue = "Created Manually",
                ChangedAt = DateTime.Now
            });

            _context.Participants.Add(entity);
            await _context.SaveChangesAsync();

            return _mapper.Map<ParticipantDto>(entity);
        }

        private readonly Dictionary<string, string> _trackedProperties = new()
        {
            { nameof(ParticipantDto.FullName), "ПІБ" },
            { nameof(ParticipantDto.Kurin), "Курінь" },
            { nameof(ParticipantDto.Email), "Email" },
            { nameof(ParticipantDto.Phone), "Телефон" },
            { nameof(ParticipantDto.IsProbeOpen), "Відкрита проба" },
            { nameof(ParticipantDto.IsMotivationLetterWritten), "Мотиваційний лист" },
            { nameof(ParticipantDto.IsFormFilled), "Заповнена форма" },
            { nameof(ParticipantDto.IsProbeContinued), "Проба продовжена" },
            { nameof(ParticipantDto.IsProbeFrozen), "Проба заморожена" },
            { nameof(ParticipantDto.IsArchived), "Архівований" },
            { nameof(ParticipantDto.ProbeOpenDate), "Дата відкриття" },
            { nameof(ParticipantDto.BirthDate), "Дата народження" },
            { nameof(ParticipantDto.Notes), "Нотатки" }
        };

        private List<ParticipantHistory> DetectChanges(Participant existingEntity, ParticipantDto newDto)
        {
            var changes = new List<ParticipantHistory>();
            var now = DateTime.Now;

            var dtoType = typeof(ParticipantDto);
            var entityType = typeof(Participant);

            foreach (var propName in _trackedProperties.Keys)
            {
                var dtoProp = dtoType.GetProperty(propName);
                var entityProp = entityType.GetProperty(propName);

                if (dtoProp == null || entityProp == null) continue;

                var newValue = dtoProp.GetValue(newDto); var oldValue = entityProp.GetValue(existingEntity);
                string sNew = FormatValue(newValue);
                string sOld = FormatValue(oldValue);

                if (sNew != sOld)
                {
                    changes.Add(new ParticipantHistory
                    {
                        PropertyName = propName,
                        OldValue = sOld,
                        NewValue = sNew,
                        ChangedAt = now
                    });
                }
            }

            return changes;
        }

        private string FormatValue(object? value)
        {
            if (value == null) return "";
            if (value is DateTime dt)
                return dt.ToString("dd.MM.yyyy");

            if (value is bool b)
                return b ? "Так" : "Ні";
            return value.ToString()?.Trim() ?? "";
        }

        private static string? LocalizeBooleanValue(string? value)
        {
            var parsed = ParseBooleanValue(value);

            if (!parsed.HasValue)
            {
                return value;
            }

            return parsed.Value ? "Так" : "Ні";
        }

        private static string ResolveArchiveAction(string? oldValue, string? newValue)
        {
            var oldArchivedState = ParseBooleanValue(oldValue);
            var newArchivedState = ParseBooleanValue(newValue);

            if (oldArchivedState == false && newArchivedState == true)
            {
                return "archived";
            }

            if (oldArchivedState == true && newArchivedState == false)
            {
                return "unarchived";
            }

            return string.Empty;
        }

        private static bool? ParseBooleanValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim().ToLowerInvariant();

            return normalized switch
            {
                "true" => true,
                "false" => false,
                "1" => true,
                "0" => false,
                "tak" => true,
                "ni" => false,
                "так" => true,
                "ні" => false,
                "yes" => true,
                "no" => false,
                _ => null
            };
        }
    }
}
