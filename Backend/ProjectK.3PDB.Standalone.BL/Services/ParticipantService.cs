using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using ProjectK._3PDB.Standalone.BL.Models;
using ProjectK._3PDB.Standalone.Infrastructure.Context;
using ProjectK._3PDB.Standalone.Infrastructure.CsvMaps;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var records = csv.GetRecords<ProjectK._3PDB.Standalone.Infrastructure.Entities.Participant>().ToList();

            foreach (var record in records)
            {
                record.History.Add(new Infrastructure.Entities.ParticipantHistory
                {
                    PropertyName = "Record",
                    OldValue = null,
                    NewValue = "Created via CSV Import",
                    ChangedAt = DateTime.Now,
                });
            }

            await _context.Participants.AddRangeAsync(records);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ParticipantDto dto)
        {
            var existingEntity = await _context.Participants
                .Include(p => p.History)
                .FirstOrDefaultAsync(p => p.ParticipantKey == dto.ParticipantKey);

            if (existingEntity == null) throw new Exception("Not found");

            // --- Перевірка змін (тут треба порівнювати entity з dto) ---
            var changes = new List<ParticipantHistory>();
            var now = DateTime.Now;

            // Приклад перевірки (порівнюємо Entity Field з DTO Property)
            CheckChange(existingEntity, "Kurin", existingEntity.Kurin, dto.Kurin, changes, now);
            CheckChange(existingEntity, "Status: IsProbeOpen", existingEntity.IsProbeOpen, dto.IsProbeOpen, changes, now);
            // ... інші поля ...

            // Оновлюємо поля Entity значеннями з DTO
            // AutoMapper тут теж може допомогти: _mapper.Map(dto, existingEntity);
            // Але обережно, щоб він не перезатер ID або колекцію історії
            _mapper.Map(dto, existingEntity);

            // Відновлюємо історію, якщо мапер її затер (або налаштувати Ignore в профілі)
            if (changes.Any())
            {
                existingEntity.History.AddRange(changes);
                await _context.SaveChangesAsync();
            }
        }

        private void CheckChange<T>(Participant entity, string propName, T oldVal, T newVal, List<ParticipantHistory> history, DateTime now)
        {
            if (!EqualityComparer<T>.Default.Equals(oldVal, newVal))
            {
                history.Add(new ParticipantHistory
                {
                    ParticipantKey = entity.ParticipantKey,
                    PropertyName = propName,
                    OldValue = oldVal?.ToString() ?? "null",
                    NewValue = newVal?.ToString() ?? "null",
                    ChangedAt = now
                });
            }
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

            return _mapper.Map<ParticipantDto>(entity); // Якщо entity null, поверне null
        }

        public async Task<List<ParticipantHistory>> GetHistoryAsync(Guid participantKey)
        {
            return await _context.ParticipantHistories
                .Where(h => h.ParticipantKey == participantKey)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<ParticipantDto> CreateAsync(ParticipantDto dto)
        {
            var entity = _mapper.Map<Participant>(dto);

            // Логіка історії
            entity.History.Add(new ParticipantHistory
            {
                PropertyName = "Record",
                NewValue = "Created Manually",
                ChangedAt = DateTime.Now
            });

            _context.Participants.Add(entity);
            await _context.SaveChangesAsync();

            // Повертаємо оновлений DTO (вже з ID)
            return _mapper.Map<ParticipantDto>(entity);
        }
    }
}
