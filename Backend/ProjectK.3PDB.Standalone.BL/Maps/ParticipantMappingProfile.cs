using AutoMapper;
using ProjectK._3PDB.Standalone.BL.Models;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjectK._3PDB.Standalone.BL.Maps;


public class ParticipantMappingProfile : Profile
{
    public ParticipantMappingProfile()
    {
        CreateMap<Participant, ParticipantDto>().ReverseMap();
        CreateMap<ParticipantHistory, ParticipantHistoryDto>().ReverseMap();
    }
}