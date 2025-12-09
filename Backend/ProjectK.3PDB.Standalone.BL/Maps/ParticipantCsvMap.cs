using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using ProjectK._3PDB.Standalone.Infrastructure.Entities;

namespace ProjectK._3PDB.Standalone.Infrastructure.CsvMaps;

public sealed class ParticipantCsvMap : ClassMap<Participant>
{
    public ParticipantCsvMap()
    {
        Map(m => m.Kurin).Name("Kurin");
        Map(m => m.FullName).Name("Full Name");
        Map(m => m.Email).Name("Email");
        Map(m => m.Phone).Name("Phone");

        Map(m => m.IsProbeOpen).Name("Відкрита проба");
        Map(m => m.IsMotivationLetterWritten).Name("Писався мотиваційний лист");
        Map(m => m.IsFormFilled).Name("Заповнена форма");
        Map(m => m.IsProbeContinued).Name("Проба продовжувалася");
        Map(m => m.IsProbeFrozen).Name("Проба заморожувалася");

        Map(m => m.ProbeOpenDate).Name("Дата відкриття проби").TypeConverterOption.Format("dd.MM.yyyy")
            .TypeConverterOption.NullValues("-", "", " ");
        Map(m => m.BirthDate).Name("Дата народження").TypeConverterOption.Format("dd.MM.yyyy")
            .TypeConverterOption.NullValues("-", "", " ");

        Map(m => m.Notes).Name("Notes");
    }
}