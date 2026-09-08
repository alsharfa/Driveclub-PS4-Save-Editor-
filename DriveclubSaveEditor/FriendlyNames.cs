namespace DriveclubSaveEditor;

internal static class FriendlyNames
{
    internal sealed record VehicleInfo(string Name, string Group, bool IsBike = false);

    internal static readonly IReadOnlyDictionary<string, VehicleInfo> Vehicles =
        new Dictionary<string, VehicleInfo>(StringComparer.Ordinal)
        {
            ["a45_amg"] = new("Mercedes-Benz A 45 AMG", "Hot Hatch"),
            ["ar_4c"] = new("Alfa Romeo 4C", "Performance"),
            ["ari_at"] = new("Ariel Atom 500 V8", "Super"),
            ["as_ma_177"] = new("Aston Martin One-77", "Super"),
            ["as_ma_2014"] = new("Aston Martin V12 Vantage S", "Super"),
            ["as_ma_vanq"] = new("Aston Martin Vanquish", "Performance"),
            ["as_za"] = new("Aston Martin V12 Zagato", "Performance"),
            ["au_a1"] = new("Audi A1 quattro", "Hot Hatch"),
            ["au_r8"] = new("Audi R8 V10 Plus", "Super"),
            ["au_rs5"] = new("Audi RS 5 Coupé", "Sports"),
            ["au_rs6"] = new("Audi RS 6 Avant", "Performance"),
            ["au_ttrs"] = new("Audi TT RS Plus", "Sports"),
            ["ba_mo"] = new("BAC Mono", "Super"),
            ["be_gtsp"] = new("Bentley Continental GT Speed", "Performance"),
            ["be_gtv8"] = new("Bentley Continental GT V8", "Sports"),
            ["bm_m1"] = new("BMW M135i", "Sports"),
            ["bm_m2"] = new("BMW M235i Coupé", "Sports"),
            ["bm_m3_gt"] = new("BMW M3 GTS", "Performance"),
            ["bm_m4"] = new("BMW M4 Coupé", "Performance"),
            ["bm_m5"] = new("BMW M5", "Performance"),
            ["bm_m5_fa"] = new("BMW M5 (Facelift)", "Performance"),
            ["cat_r500"] = new("Caterham R500 Superlight", "Performance"),
            ["cat_sp300"] = new("Caterham SP/300.R", "Super"),
            ["che_ca"] = new("Chevrolet Camaro ZL1", "Performance"),
            ["che_co_c7_z06"] = new("Chevrolet Corvette C7 Z06", "Super"),
            ["che_co_st"] = new("Chevrolet Corvette C7 Stingray", "Performance"),
            ["che_co_z0"] = new("Chevrolet Corvette C6 Z06", "Performance"),
            ["che_co_zr1"] = new("Chevrolet Corvette C6 ZR1", "Super"),
            ["cit_surv"] = new("DS Survolt", "Performance"),
            ["cla_45"] = new("Mercedes-Benz CLA 45 AMG", "Sports"),
            ["do_chal"] = new("Dodge Challenger SRT8 392", "Performance"),
            ["do_char"] = new("Dodge Charger SRT8", "Performance"),
            ["do_vi"] = new("SRT Viper GTS", "Super"),
            ["fe_430"] = new("Ferrari 430 Scuderia", "Super"),
            ["fe_458"] = new("Ferrari 458 Italia", "Super"),
            ["fe_458_spe"] = new("Ferrari 458 Speciale", "Super"),
            ["fe_488_gtb"] = new("Ferrari 488 GTB", "Super"),
            ["fe_599"] = new("Ferrari 599XX Evoluzione", "Hyper"),
            ["fe_599_gto"] = new("Ferrari 599 GTO", "Super"),
            ["fe_ca"] = new("Ferrari California", "Performance"),
            ["fe_enz"] = new("Ferrari Enzo Ferrari", "Hyper"),
            ["fe_f12"] = new("Ferrari F12berlinetta", "Super"),
            ["fe_f40"] = new("Ferrari F40", "Super"),
            ["fe_f50"] = new("Ferrari F50", "Super"),
            ["fe_ff"] = new("Ferrari FF", "Performance"),
            ["fe_fxx_ev"] = new("Ferrari FXX Evoluzione", "Hyper"),
            ["fe_laf"] = new("Ferrari LaFerrari", "Hyper"),
            ["fe_laf_fxxk"] = new("Ferrari FXX K", "Hyper"),
            ["gta_spa"] = new("Spania GTA Spano", "Super"),
            ["gu_ap"] = new("Gumpert Apollo Enraged", "Hyper"),
            ["he_ve_gt"] = new("Hennessey Venom GT", "Hyper"),
            ["ico_vul"] = new("Icona Vulcano", "Super"),
            ["jag_cx75"] = new("Jaguar C-X75", "Hyper"),
            ["jag_f_type_r"] = new("Jaguar F-Type R Coupé", "Performance"),
            ["jag_xkrs"] = new("Jaguar XKR-S Coupé", "Performance"),
            ["jag_xj220"] = new("Jaguar XJ220", "Super"),
            ["ko_ag"] = new("Koenigsegg Agera R", "Hyper"),
            ["ko_one_1"] = new("Koenigsegg One:1", "Hyper"),
            ["ko_reg"] = new("Koenigsegg Regera", "Hyper"),
            ["ktm_xbow_r"] = new("KTM X-BOW R", "Super"),
            ["lam_av50"] = new("Lamborghini Aventador LP 720-4 50° Anniversario", "Super"),
            ["lam_dia"] = new("Lamborghini Diablo SV", "Super"),
            ["lam_gal"] = new("Lamborghini Gallardo LP 570-4 Squadra Corse", "Super"),
            ["lam_hur"] = new("Lamborghini Huracán LP 610-4", "Super"),
            ["lam_mur"] = new("Lamborghini Murciélago LP 670-4 SV", "Super"),
            ["lam_rev"] = new("Lamborghini Reventón", "Super"),
            ["lam_ses"] = new("Lamborghini Sesto Elemento", "Hyper"),
            ["lam_ven"] = new("Lamborghini Veneno", "Hyper"),
            ["lo_ev"] = new("Lotus Evora S Sports Racer", "Sports"),
            ["lo_ex"] = new("Lotus Exige S", "Performance"),
            ["ma_mc_st"] = new("Maserati GranTurismo MC Stradale", "Performance"),
            ["mar_b2"] = new("Marussia B2", "Super"),
            ["maz_eva"] = new("Mazzanti Evantra", "Super"),
            ["mc_570s"] = new("McLaren 570S", "Super"),
            ["mc_650s"] = new("McLaren 650S Coupé", "Super"),
            ["mc_f1_lm"] = new("McLaren F1 LM", "Hyper"),
            ["mc_mp_12"] = new("McLaren 12C", "Super"),
            ["mc_p1"] = new("McLaren P1", "Hyper"),
            ["mc_p1_gtr"] = new("McLaren P1 GTR", "Hyper"),
            ["me_amg_gt3"] = new("Mercedes-AMG GT3", "Super"),
            ["me_c63"] = new("Mercedes-Benz C 63 AMG Coupé Black Series", "Performance"),
            ["me_gt_am"] = new("Mercedes-AMG GT S", "Performance"),
            ["me_s65_amg"] = new("Mercedes-AMG S 65 Coupé", "Performance"),
            ["me_sl65"] = new("Mercedes-Benz SL 65 AMG 45th Anniversary Edition", "Performance"),
            ["me_sl_am"] = new("Mercedes-Benz SLS AMG Coupé Black Series", "Super"),
            ["me_sl_am_ca"] = new("Mercedes-Benz SLS AMG Coupé Black Series (Pre-order)", "Super"),
            ["me_sl_am_edrv"] = new("Mercedes-Benz SLS AMG Coupé Electric Drive", "Super"),
            ["mi_jcw"] = new("MINI John Cooper Works GP", "Hot Hatch"),
            ["nis_370"] = new("Nissan 370Z NISMO", "Performance"),
            ["nis_gtr"] = new("Nissan GT-R", "Performance"),
            ["nis_gtr_nis"] = new("Nissan GT-R NISMO", "Super"),
            ["pa_hu"] = new("Pagani Huayra", "Hyper"),
            ["pa_zo"] = new("Pagani Zonda R", "Hyper"),
            ["pe_ex1"] = new("Peugeot EX1 Concept", "Performance"),
            ["pe_onyx"] = new("Peugeot Onyx Concept", "Hyper"),
            ["ren_alp"] = new("Renault Alpine A110-50", "Super"),
            ["ren_cl"] = new("Renault Clio R.S. 200 EDC", "Hot Hatch"),
            ["ren_dez"] = new("Renault DeZir DRIVECLUB Edition", "Sports"),
            ["ren_meg_tro_r"] = new("Renault Mégane R.S. 275 Trophy-R", "Hot Hatch"),
            ["ren_rs01"] = new("Renault Sport R.S. 01", "Super"),
            ["ren_twin"] = new("Renault Twin'Run Concept", "Super"),
            ["ri_c1"] = new("Rimac Automobili Concept One", "Hyper"),
            ["ru_rt12r"] = new("RUF RT12 R", "Super"),
            ["ru_rt12r_pre"] = new("RUF RT12 R (Pre-order)", "Super"),
            ["ruf_ctr"] = new("RUF CTR3 Clubsport", "Hyper"),
            ["ruf_rgt"] = new("RUF RGT 8", "Performance"),
            ["sav_riv"] = new("Savage Rivale GTR-S", "Super"),
            ["sp_c8"] = new("Spyker C8 Aileron", "Performance"),
            ["vu_05"] = new("VUHL 05", "Super"),
            ["vw_beetle_gsr"] = new("Volkswagen Beetle GSR", "Hot Hatch"),
            ["vw_des_vis_gti"] = new("Volkswagen Design Vision GTI Concept", "Performance"),
            ["vw_golf_gti"] = new("Volkswagen Golf GTI", "Hot Hatch"),
            ["wm_ly_hy"] = new("W Motors Lykan HyperSport", "Hyper"),
            ["wo_typ"] = new("Wombat Typhoon", "Bonus"),

            ["bi_bb3"] = new("Bimota BB3", "Superbike", true),
            ["bmw_s1000_rr_sp"] = new("BMW S1000RR", "Superbike", true),
            ["duc_1098"] = new("Ducati 1098 R", "Superbike", true),
            ["duc_1299_pan_s"] = new("Ducati 1299 Panigale S", "Superbike", true),
            ["duc_desmo_rr"] = new("Ducati Desmosedici RR", "Superbike", true),
            ["ebr_1190_rx"] = new("EBR 1190RX", "Superbike", true),
            ["ebr_1190_sx"] = new("EBR 1190SX", "Superbike", true),
            ["hon_cbr1000_rr_sp"] = new("Honda CBR1000RR Fireblade SP", "Superbike", true),
            ["kaw_h2"] = new("Kawasaki Ninja H2", "Superbike", true),
            ["kaw_zx10_r"] = new("Kawasaki Ninja ZX-10R 30th Anniversary", "Superbike", true),
            ["ktm_1290_sd"] = new("KTM 1290 Super Duke R", "Superbike", true),
            ["ktm_rc8_r"] = new("KTM RC8 R", "Superbike", true),
            ["mv_agusta_f4_rr"] = new("MV Agusta F4 RR", "Superbike", true),
            ["suz_gsxr_1000"] = new("Suzuki GSX-R1000", "Superbike", true),
            ["suz_gsx_s1000"] = new("Suzuki GSX-S1000", "Superbike", true),
            ["yam_yzf_r1"] = new("Yamaha YZF-R1", "Superbike", true),
            ["yam_yzf_r1m"] = new("Yamaha YZF-R1M", "Superbike", true),
            ["mv_brut_1090"] = new("MV Agusta Brutale 1090 Corsa", "Superbike", true),
            ["mv_agusta_f4_rc"] = new("MV Agusta F4 RC", "Superbike", true),
            ["apr_rsv4_rf"] = new("Aprilia RSV4 RF", "Superbike", true),
        };

    internal static readonly IReadOnlyDictionary<string, string> Tracks =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["canada_road_point_01_n"] = "Cayoosh Point",
            ["canada_road_point_01_r"] = "Cayoosh Point — Reverse",
            ["canada_road_point_03_n"] = "Lytton",
            ["canada_road_point_03_r"] = "Lytton — Reverse",
            ["canada_road_circuit_01_n"] = "Fraser Valley",
            ["canada_road_circuit_01_r"] = "Fraser Valley — Reverse",
            ["canada_road_circuit_02_n"] = "Oliver's Landing",
            ["canada_road_circuit_02_r"] = "Oliver's Landing — Reverse",
            ["canada_dlc_road_circuit_03_n"] = "Sinclair Pass",
            ["canada_dlc_road_circuit_03_r"] = "Sinclair Pass — Reverse",
            ["canada_race_track_v1"] = "Maplewood Motorsport Park V1",
            ["canada_race_track_v2"] = "Maplewood Motorsport Park V2",
            ["canada_race_track_v3"] = "Maplewood Motorsport Park V3",

            ["chile_road_point_01_n"] = "Putre",
            ["chile_road_point_01_r"] = "Putre — Reverse",
            ["chile_road_point_04_n"] = "Taapaca",
            ["chile_road_point_04_r"] = "Taapaca — Reverse",
            ["chile_road_circuit_01_n"] = "Chungará Lake",
            ["chile_road_circuit_01_r"] = "Chungará Lake — Reverse",
            ["chile_road_circuit_02_n"] = "Salar De Surire",
            ["chile_road_circuit_02_r"] = "Salar De Surire — Reverse",
            ["chile_dlc_road_circuit_01_n"] = "Los Pelambres",
            ["chile_dlc_road_circuit_01_r"] = "Los Pelambres — Reverse",
            ["chile_race_track_v1"] = "Autodromo Frontera V1",
            ["chile_race_track_v2"] = "Autodromo Frontera V2",
            ["chile_race_track_v3"] = "Autodromo Frontera V3",

            ["india_road_point_01_n"] = "Munnar",
            ["india_road_point_01_r"] = "Munnar — Reverse",
            ["india_road_point_02_n"] = "Nilgiri Hills",
            ["india_road_point_02_r"] = "Nilgiri Hills — Reverse",
            ["india_road_circuit_01_n"] = "Bandipur",
            ["india_road_circuit_01_r"] = "Bandipur — Reverse",
            ["india_road_circuit_02_n"] = "Glenmorgan",
            ["india_road_circuit_02_r"] = "Glenmorgan — Reverse",
            ["india_dlc_road_circuit_03_n"] = "Yedapalli",
            ["india_dlc_road_circuit_03_r"] = "Yedapalli — Reverse",
            ["india_race_track_v1"] = "Tamil Nadu Circuit V1",
            ["india_race_track_v2"] = "Tamil Nadu Circuit V2",
            ["india_race_track_v3"] = "Tamil Nadu Circuit V3",

            ["japan_road_point_01_n"] = "Goshodaira",
            ["japan_road_point_01_r"] = "Goshodaira — Reverse",
            ["japan_road_point_02_n"] = "Nakasendo",
            ["japan_road_point_02_r"] = "Nakasendo — Reverse",
            ["japan_road_circuit_01_n"] = "Lake Shoji",
            ["japan_road_circuit_01_r"] = "Lake Shoji — Reverse",
            ["japan_road_circuit_02_n"] = "Takahagi Hills",
            ["japan_road_circuit_02_r"] = "Takahagi Hills — Reverse",
            ["japan_dlc_road_circuit_03_n"] = "Kobago",
            ["japan_dlc_road_circuit_03_r"] = "Kobago — Reverse",
            ["japan_race_track_01_v1"] = "Asagiri Hills Racetrack V1",
            ["japan_race_track_01_v2"] = "Asagiri Hills Racetrack V2",
            ["japan_race_track_01_v3"] = "Asagiri Hills Racetrack V3",

            ["norway_road_point_01_n"] = "Holmastad",
            ["norway_road_point_01_r"] = "Holmastad — Reverse",
            ["norway_road_point_03_n"] = "Hurrungane",
            ["norway_road_point_03_r"] = "Hurrungane — Reverse",
            ["norway_road_circuit_01_n"] = "Sentraltind",
            ["norway_road_circuit_01_r"] = "Sentraltind — Reverse",
            ["norway_road_circuit_02_n"] = "Skjolden",
            ["norway_road_circuit_02_r"] = "Skjolden — Reverse",
            ["norway_dlc_road_circuit_03_n"] = "Atlanterhavsvegen",
            ["norway_dlc_road_circuit_03_r"] = "Atlanterhavsvegen — Reverse",
            ["norway_race_track_01_v1"] = "SKNO Sognefjord Raceway V1",
            ["norway_race_track_01_v2"] = "SKNO Sognefjord Raceway V2",
            ["norway_race_track_01_v3"] = "SKNO Sognefjord Raceway V3",

            ["uk_road_point_01_n"] = "The Kyle",
            ["uk_road_point_01_r"] = "The Kyle — Reverse",
            ["uk_road_point_02_n"] = "Trotternish",
            ["uk_road_point_02_r"] = "Trotternish — Reverse",
            ["uk_road_circuit_01_n"] = "Kinloch",
            ["uk_road_circuit_01_r"] = "Kinloch — Reverse",
            ["uk_road_circuit_02_n"] = "Loch Duich",
            ["uk_road_circuit_02_r"] = "Loch Duich — Reverse",
            ["uk_dlc_road_circuit_03_n"] = "Wester Ross",
            ["uk_dlc_road_circuit_03_r"] = "Wester Ross — Reverse",
            ["uk_race_track_v1"] = "Black Hills Circuit V1",
            ["uk_race_track_v2"] = "Black Hills Circuit V2",
            ["uk_race_track_v3"] = "Black Hills Circuit V3",
            ["uk_dlc_road_circuit_04_v1_n"] = "Old Town V1",
            ["uk_dlc_road_circuit_04_v1_r"] = "Old Town V1 — Reverse",
            ["uk_dlc_road_circuit_04_v2_n"] = "Old Town V2",
            ["uk_dlc_road_circuit_04_v2_r"] = "Old Town V2 — Reverse",
            ["uk_dlc_road_circuit_04_v3_n"] = "Old Town V3",
            ["uk_dlc_road_circuit_04_v3_r"] = "Old Town V3 — Reverse",
        };

    internal static string GetTrack(string code) =>
        Tracks.TryGetValue(code, out var name) ? name : Humanize(code);

    internal static readonly IReadOnlyDictionary<string, string> FeatureLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["garage"] = "Garage",
            ["race"] = "Race",
            ["drift"] = "Drift",
            ["sprint"] = "Sprint",
            ["timetrial"] = "Time Trial",
            ["topspeed"] = "Top Speed",
            ["multiplayer"] = "Multiplayer menu",
            ["socialhub"] = "Social Hub",
            ["photomode"] = "Photo Mode",
            ["faceoff"] = "Face-Offs",
            ["accolades"] = "Accolades",
            ["fame"] = "Fame screen",
            ["challenges.solo"] = "Solo Challenges",
            ["club.browse"] = "Browse Clubs",
            ["club.me"] = "My Club",
            ["kers.drs"] = "KERS / DRS controls",
            ["home"] = "Home",
            ["profile.me"] = "My Profile",
            ["startline"] = "Start Line",
            ["toureasy"] = "Tour Easy option",
        };

    internal static VehicleInfo GetVehicle(string code) =>
        Vehicles.TryGetValue(code, out var info)
            ? info
            : new VehicleInfo(Humanize(code), "Unknown");

    internal static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        string s = value.Replace('_', ' ').Replace('#', ' ');
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
    }

    internal static int GetVehicleLevel(string code, ulong fame)
    {
        var thresholds = GetVehicleThresholds(code);
        int level = 1;
        for (int i = 0; i < thresholds.Count; i++)
        {
            if (fame >= thresholds[i]) level = i + 1;
            else break;
        }
        return Math.Clamp(level, 1, thresholds.Count);
    }

    internal static ulong GetVehicleThreshold(string code, int level)
    {
        var thresholds = GetVehicleThresholds(code);
        int index = Math.Clamp(level, 1, thresholds.Count) - 1;
        return thresholds[index];
    }

    private static IReadOnlyList<ulong> GetVehicleThresholds(string code)
    {
        int column = -1;
        for (int i = 0; i < ProgressionData.VehicleColumns.Count; i++)
        {
            if (string.Equals(ProgressionData.VehicleColumns[i], code, StringComparison.Ordinal))
            {
                column = i;
                break;
            }
        }

        if (column < 0)
        {
            bool isBike = GetVehicle(code).IsBike;
            string fallback = isBike ? "CLASS_Superbike" : "CLASS_Performance";
            for (int i = 0; i < ProgressionData.VehicleColumns.Count; i++)
            {
                if (string.Equals(ProgressionData.VehicleColumns[i], fallback, StringComparison.Ordinal))
                {
                    column = i;
                    break;
                }
            }
        }

        if (column < 0)
            throw new InvalidDataException($"No vehicle progression column is available for '{code}'.");

        return ProgressionData.VehicleLevels.Select(x => x.Thresholds[column]).ToArray();
    }
}
