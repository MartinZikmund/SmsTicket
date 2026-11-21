using SmsTicket.Data.Models;

namespace SmsTicket.Data;

// Wikimedia Commons photos, see Assets/Cities/attributions.json
public static class PhotoCredits
{
    private static readonly Dictionary<string, PhotoCredit> _credits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BR"] = new("Kirk", "CC BY-SA 3.0", "http://creativecommons.org/licenses/by-sa/3.0/", "https://commons.wikimedia.org/wiki/File:Brno-Tram-4.jpg", 0.4),
        ["CHEB"] = new("Pavel Špindler", "CC BY 3.0", "https://creativecommons.org/licenses/by/3.0", "https://commons.wikimedia.org/wiki/File:Cheb_-_%C5%A0pal%C3%AD%C4%8Dek_-_panoramio.jpg", 0.3),
        ["HK"] = new("Michielverbeek", "CC BY-SA 4.0", "https://creativecommons.org/licenses/by-sa/4.0", "https://commons.wikimedia.org/wiki/File:Hradec_Kr%C3%A1lov%C3%A9,_de_B%C3%ADl%C3%A1_v%C4%9B%C5%BE_en_het_stadhuis_Dm406206-368_IMG_6854_2018-08-06_09.46.jpg", 0.42),
        ["LBC"] = new("VitVit", "CC BY-SA 4.0", "https://creativecommons.org/licenses/by-sa/4.0", "https://commons.wikimedia.org/wiki/File:Liberec_n%C3%A1m%C4%9Bst%C3%AD_a_radnice_6.jpg", 0.5),
        ["OL"] = new("Pudelek", "CC BY-SA 4.0", "https://creativecommons.org/licenses/by-sa/4.0", "https://commons.wikimedia.org/wiki/File:Olomouc,_Horn%C3%AD_n%C3%A1m%C4%9Bst%C3%AD_(2017).jpg", 0.3),
        ["OST"] = new("ŠJů", "CC BY-SA 3.0", "https://creativecommons.org/licenses/by-sa/3.0", "https://commons.wikimedia.org/wiki/File:Moravsk%C3%A1_Ostrava,_d%C5%AFl_Hlubina_od_%C5%BDelez%C3%A1rensk%C3%A9,_tramvaj_Vario_LF.jpg", 0.3),
        ["PAR"] = new("Oleksandr Dede", "CC BY-SA 4.0", "https://creativecommons.org/licenses/by-sa/4.0", "https://commons.wikimedia.org/wiki/File:Pardubice,_%C5%A0koda_32Tr_SOR_%C4%8D._464_Zelen%C3%A1_br%C3%A1na.jpg", 0.35),
        ["PIL"] = new("Smiley.toerist", "CC BY-SA 4.0", "https://creativecommons.org/licenses/by-sa/4.0", "https://commons.wikimedia.org/wiki/File:Pilsen_tram_2024_3.jpg", 0.4),
        ["PHA"] = new("hrs51", "CC BY 4.0", "https://creativecommons.org/licenses/by/4.0", "https://commons.wikimedia.org/wiki/File:Prague_2010_Tatra_T3_and_Hradschin.jpg", 0.46),
        ["TB"] = new("Michielverbeek", "CC BY-SA 4.0", "https://creativecommons.org/licenses/by-sa/4.0", "https://commons.wikimedia.org/wiki/File:T%C3%A1bor,_stadszicht_met_de_Jord%C3%A1n_op_de_voorgrond_IMG_6377_2018-08-02_09.30.jpg", 0.33),
        ["ZL"] = new("Pavel Langer", "CC BY-SA 3.0", "https://creativecommons.org/licenses/by-sa/3.0", "https://commons.wikimedia.org/wiki/File:21.budova.jpg", 0.4),
        ["UL"] = new("RomanM82", "CC BY-SA 4.0", "https://creativecommons.org/licenses/by-sa/4.0", "https://commons.wikimedia.org/wiki/File:%C3%9Ast%C3%AD_nad_Labem_-_Mari%C3%A1nsk%C3%BD_most,_pohled_z_mostu_E._Bene%C5%A1e_obr02.jpg", 0.35),
        ["CB"] = new("Jakub Hałun", "CC BY 4.0", "https://creativecommons.org/licenses/by/4.0", "https://commons.wikimedia.org/wiki/File:King_P%C5%99emysl_Otakar_II_Square_in_%C4%8Cesk%C3%A9_Bud%C4%9Bjovice,_20250504_1557_8938.jpg", 0.45),
    };

    public static PhotoCredit? ForCity(string cityId) => _credits.GetValueOrDefault(cityId);
}
