using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using ProjectK._3PDB.Standalone.BL.Models;
using ProjectK._3PDB.Standalone.Infrastructure.Context;
using ProjectK._3PDB.Standalone.Infrastructure.CsvMaps;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;
using System.Globalization;

namespace ProjectK._3PDB.Standalone.BL.Services
{
    public class ParticipantService
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

        public async Task UpdateAsync(ParticipantDto dto)
        {
            var existingEntity = await _context.Participants
                .Include(p => p.History)
                .FirstOrDefaultAsync(p => p.ParticipantKey == dto.ParticipantKey);

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
                        .AsNoTracking()
                        .ToListAsync();

            return _mapper.Map<List<ParticipantDto>>(entities);
        }

        public async Task<ParticipantDto?> GetByKeyAsync(Guid participantKey)
        {
            var entity = await _context.Participants
                        .Include(p => p.History)
                        .FirstOrDefaultAsync(p => p.ParticipantKey == participantKey);

            return _mapper.Map<ParticipantDto>(entity);
        }

        public async Task<List<ParticipantHistory>> GetHistoryAsync(Guid participantKey)
        {
            return await _context.ParticipantHistories
                .Where(h => h.ParticipantKey == participantKey)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task DeleteAsync (Guid participantKey)
        {
            var entity = await _context.Participants
                        .FirstOrDefaultAsync(p => p.ParticipantKey == participantKey);
            if (entity == null) throw new Exception("Not found");
            _context.Participants.Remove(entity);
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
                return b ? "Tak" : "Ni";
            return value.ToString()?.Trim() ?? "";
        }
    }
}
