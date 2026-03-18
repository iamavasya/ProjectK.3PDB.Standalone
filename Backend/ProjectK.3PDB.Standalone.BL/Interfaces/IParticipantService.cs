using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ProjectK._3PDB.Standalone.BL.Models;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;

namespace ProjectK._3PDB.Standalone.BL.Interfaces
{
    public interface IParticipantService
    {
        Task ImportCsvAsync(Stream fileStream);
        Task<byte[]> ExportCsvAsync();
        Task UpdateAsync(ParticipantDto dto);
        Task<List<ParticipantDto>> GetAllAsync();
        Task<ParticipantDto?> GetByKeyAsync(Guid participantKey);
        Task<List<ParticipantHistory>> GetHistoryAsync(Guid participantKey);
        Task DeleteAsync(Guid participantKey);
        Task<ParticipantDto> CreateAsync(ParticipantDto dto);
    }
}
