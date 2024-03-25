using SmsTicket.Data.Models;
using SmsTicket.Services.Localizer;

namespace SmsTicket.Data;

public static class DataSource
{
    private static readonly City[] _cities;

    static DataSource()
    {
        var localizerService = new LocalizationService();
        _cities = new City[]
        {
                new City
                {
                    Id = "PHA",
                    Abbreviation = localizerService["PragueAbbreviation"],
                    Name = localizerService["PragueName"],
                    DefaultTicketId = "PHA42",
                    TicketTypes = new()
                    {
                        new PragueTicketType("PHA31", "", "31", "30 min", "DPT31"),
                        new PragueTicketType("PHA42", "", "42", "90 min", "DPT42"),
                        new PragueTicketType("PHA120", localizerService["PragueRequiresConfirmationSms"], "120",
                            "24 h", "DPT120"),
                        new PragueTicketType("PHA330", localizerService["PragueRequiresConfirmationSms"], "330",
                            "72 h", "DPT330"),
                        new("PHADPTA", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "DPTA", true)
                    }
                },
                new City
                {
                    Id = "LBC",
                    Abbreviation = "LBC",
                    Name = "Liberec",
                    DefaultTicketId = "LIB",
                    TicketTypes = new()
                    {
                        new("LIB", "", "38", "60 min", "90206", "LIB", true),
                        new("LIBDupe", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "LIB", true)
                    }
                },
                new City
                {
                    Id = "CB",
                    Abbreviation = "ČB",
                    Name = "České Budějovice",
                    DefaultTicketId = "BUD25",
                    TicketTypes = new()
                    {
                        new("BUD25", "", "20", "60 min", "90206", "BUD", true),
                        new("BUD70", "", "75", "24 h", "90206", "BUD24", true),
                        new("BUDDupe60", localizerService["DuplicateBud60SmsDetail"], "3", "", "9000603", "BUD", true),
                        new("BUDDupe24", localizerService["DuplicateBud24SmsDetail"], "3", "", "9000603", "BUD24", true)
                    }
                },
                new City
                {
                    Id = "HK",
                    Abbreviation = "HK",
                    Name = "Hradec Králové",
                    DefaultTicketId = "HK",
                    TicketTypes = new()
                    {
                        new("HK", "", "30", "60 min", "90230", "HK", true),
                        new("HKZ", localizerService["ReducedFare"], "15", "60 min", "90230", "HKZ", true),
                        new("HK24", "", "100", "24 h", "90230", "HK24", true),
                        new("HKDupe", localizerService["DuplicateSmsDetail"], "5", "", "90030", "HKD", true)

                    }
                },
                new City
                {
                    Id = "KV",
                    Abbreviation = "KV",
                    Name = localizerService["CarlsbadName"],
                    DefaultTicketId = "JKV25",
                    TicketTypes = new()
                    {
                        new("JKV30", "", "20", "60 min", "90230", "JKV20", true),
                        new("JKV17", localizerService["ReducedFare"], "40", "60 min", "90230", "JKV40", true),
                        new("JKVDupe", localizerService["DuplicateSmsDetail"], "5", "", "90030", "JKVD", true)
                    }
                },
                new City
                {
                    Id = "OL",
                    Abbreviation = "OL",
                    Name = "Olomouc",
                    DefaultTicketId = "DPMO",
                    TicketTypes = new()
                    {
                        new("DPMO", localizerService["OLDurationDetail"], "22", "50 min", "90206", "DPMO", true),
                        new("DPMODupe", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "DPMO", true),
                    }
                },
                new City
                {
                    Id = "OST",
                    Abbreviation = "OST",
                    Name = "Ostrava",
                    DefaultTicketId = "DPO70",
                    TicketTypes = new()
                    {
                        new("DPO70", "", "35", "70 min", "90206", "DPO70", true),
                        new("DPO24", "", "100", "24 h", "90206", "DPO24", true),
                        new("DPO70Z", localizerService["ReducedFare"], "17", "70 min", "90206", "DPO70Z", true),
                        new("DPO24Z", localizerService["ReducedFare"], "50", "24 h", "90206", "DPO24Z", true),
                        new("DPODupe", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "DPOD", true),
                    }
                },

                new City
                {
                    Id = "PAR",
                    Abbreviation = "PAR",
                    Name = "Pardubice",
                    DefaultTicketId = "DPMP",
                    TicketTypes = new()
                    {
                        new("DPMP", localizerService["PARDurationDetail"], "35", "45 min", "90206", "DPMP", true),
                        new("DPMP24", "", "60", "24 h", "90206", "DPMP24", true),
                        new("DPMPDUPE", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "DPMP", true),
                    }
                },
                new City
                {
                    Id = "PIL",
                    Abbreviation = localizerService["PILAbbreviation"],
                    Name = localizerService["PILName"],
                    DefaultTicketId = "PMDP35M",
                    TicketTypes = new()
                    {
                        new("PMDP35M", localizerService["PILInnerZone"], "28", "35 min", "90206", "PMDP35M", true),
                        new("PMDP24H", localizerService["PILInnerZone"], "99", "24 h", "90206", "PMDP24H", true),
                        new("PILDUPE", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "PMDPD", true),
                    }
                },
                new City
                {
                    Id = "SOK",
                    Abbreviation = "SOK",
                    Name = "Sokolov",
                    DefaultTicketId = "SOK",
                    TicketTypes = new()
                    {
                        new("SOK", localizerService["SOKDurationDetail"], "30", "20 h", "90206", "SOK", true),
                        new("SOKDupe", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "SOKD", true),
                    }
                },
                new City
                {
                    Id = "UL",
                    Abbreviation = "ÚnL",
                    Name = "Ústí nad Labem",
                    DefaultTicketId = "MDJ",
                    TicketTypes = new()
                    {
                        new("MDJ", "", "26", "60 min", "90206", "MDJ", true),
                        new("MDJ106", "", "106", "24 h", "90206", "MDJ106", true),
                        new("MDJZD", localizerService["ReducedFareUL"], "53", "24 h", "90206", "MDJZD", true),
                        new("MDJ60", localizerService["ReducedFareUL2"], "60", "24 h", "90206", "MDJ60", true),
                        new("MDJDupe", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "MDJA", true),
                    }
                },
                new City
                {
                    Id = "ZL",
                    Abbreviation = "ZL,O",
                    Name = "Zlín a Otrokovice",
                    DefaultTicketId = "DSZO",
                    TicketTypes = new()
                    {
                        new("DSZO", "", "30", "70 min", "90206", "DSZO", true),
                        new("DSZOZ", localizerService["ReducedFare"], "15", "70 min", "90206", "DSZOZ", true),
                        new("DSZO24", "", "100", "24 h", "90206", "DSZO24", true),
                        new("DSZO24Z", localizerService["ReducedFare"], "50", "24 h", "90206", "DSZO24Z", true),
                        new("DSZODupe", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "DSZO", true),
                    }
                },
                new City
                {
                    Id = "BR",
                    Abbreviation = "BR",
                    Name = "Brno",
                    DefaultTicketId = "BR29",
                    TicketTypes = new()
                    {
                        new BrnoTicketType("BR20", "", "20", "20 min", "BRNO20"),
                        new BrnoTicketType("BR29", "", "29", "75 min", "BRNO"),
                        new BrnoTicketType("BR99", "", "99", "24 h", "BRNOD"),
                        new("BRDupe", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "BRNOA", true)
                    }
                },
                new City
                {
                    Id ="TB",
                    Abbreviation = "TB",
                    Name = "Tábor",
                    DefaultTicketId = "COM",
                    TicketTypes = new()
                    {
                        new("COM", "", "24", "60 min", "90206", "COM", true),
                        new("COMZ", localizerService["ReducedFare"], "12", "60 min", "90206", "COMZ", true),
                        new("COMDupe", localizerService["DuplicateSmsDetail"], "3", "", "9000603", "COMA", true)
                    }
                }
        }
        .OrderBy(d => d.Name)
        .ToArray();
    }

    public static IReadOnlyList<City> Cities => _cities.AsReadOnly();
}
