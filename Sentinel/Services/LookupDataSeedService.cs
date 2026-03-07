using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Services
{
    public static class LookupDataSeedService
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            logger.LogInformation("Starting lookup data seeding...");

            await SeedLanguagesAsync(context, logger);
            await SeedContactClassificationsAsync(context, logger);
            await SeedAtsiStatusesAsync(context, logger);
            await SeedSexAtBirthAsync(context, logger);
            await SeedGendersAsync(context, logger);
            await SeedCaseStatusesAsync(context, logger);
            await SeedDiseaseCategoriesAsync(context, logger);
            await SeedOrganizationTypesAsync(context, logger);
            await SeedSpecimenTypesAsync(context, logger);
            await SeedTestTypesAsync(context, logger);
            await SeedTestResultsAsync(context, logger);
            await SeedSymptomsAsync(context, logger);
            await SeedTaskTypesAsync(context, logger);

            logger.LogInformation("Lookup data seeding finished successfully");
        }

        private static async Task SeedLanguagesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Languages.AnyAsync())
            {
                logger.LogInformation("Languages table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Languages...");

            var languages = new List<Language>
            {
                // Aboriginal and Torres Strait Islander Languages
                new Language { Code = "11111111", Name = "Adnyamathanha", IsActive = true },
                new Language { Code = "11111112", Name = "Barngarla", IsActive = true },
                new Language { Code = "11111113", Name = "Kaurna", IsActive = true },
                new Language { Code = "11111114", Name = "Narungga", IsActive = true },
                new Language { Code = "11111115", Name = "Nukunu", IsActive = true },
                new Language { Code = "11111211", Name = "Antikarinya", IsActive = true },
                new Language { Code = "11111212", Name = "Kokatha", IsActive = true },
                new Language { Code = "11111213", Name = "Kukatja", IsActive = true },
                new Language { Code = "11111214", Name = "Luritja", IsActive = true },
                new Language { Code = "11111215", Name = "Manyjilyjarra", IsActive = true },
                new Language { Code = "11111216", Name = "Martu Wangka", IsActive = true },
                new Language { Code = "11111217", Name = "Ngaanyatjarra", IsActive = true },
                new Language { Code = "11111218", Name = "Pintupi", IsActive = true },
                new Language { Code = "11111221", Name = "Pitjantjatjara", IsActive = true },
                new Language { Code = "11111222", Name = "Tjupan", IsActive = true },
                new Language { Code = "11111223", Name = "Wangkajunga", IsActive = true },
                new Language { Code = "11111224", Name = "Wangkatha", IsActive = true },
                new Language { Code = "11111225", Name = "Warnman", IsActive = true },
                new Language { Code = "11111226", Name = "Yankunytjatjara", IsActive = true },
                new Language { Code = "11111227", Name = "Yulparija", IsActive = true },
                new Language { Code = "11111299", Name = "South West Languages, Western Desert, nec", IsActive = true },
                new Language { Code = "11111311", Name = "Banyjima", IsActive = true },
                new Language { Code = "11111312", Name = "Kariyarra", IsActive = true },
                new Language { Code = "11111313", Name = "Ngarla", IsActive = true },
                new Language { Code = "11111314", Name = "Ngarluma", IsActive = true },
                new Language { Code = "11111315", Name = "Nyamal", IsActive = true },
                new Language { Code = "11111316", Name = "Palyku", IsActive = true },
                new Language { Code = "11111317", Name = "Yindjibarndi", IsActive = true },
                new Language { Code = "11111318", Name = "Yinhawangka", IsActive = true },
                new Language { Code = "11111411", Name = "Mangala", IsActive = true },
                new Language { Code = "11111412", Name = "Karajarri", IsActive = true },
                new Language { Code = "11111413", Name = "Nyangumarta", IsActive = true },
                new Language { Code = "11111511", Name = "Gurindji", IsActive = true },
                new Language { Code = "11111512", Name = "Jaru", IsActive = true },
                new Language { Code = "11111513", Name = "Mudburra", IsActive = true },
                new Language { Code = "11111514", Name = "Ngarinyman", IsActive = true },
                new Language { Code = "11111515", Name = "Walmajarri", IsActive = true },
                new Language { Code = "11111516", Name = "Warlmanpa", IsActive = true },
                new Language { Code = "11111517", Name = "Warlpiri", IsActive = true },
                new Language { Code = "11111599", Name = "South West Languages, Northern Desert Fringe, nec", IsActive = true },
                new Language { Code = "11111611", Name = "Mirniny", IsActive = true },
                new Language { Code = "11111612", Name = "Noongar", IsActive = true },
                new Language { Code = "11111613", Name = "Thalandji", IsActive = true },
                new Language { Code = "11111699", Name = "Other South West Languages, nec", IsActive = true },
                new Language { Code = "11111711", Name = "Badimaya", IsActive = true },
                new Language { Code = "11111712", Name = "Wajarri", IsActive = true },
                new Language { Code = "11111811", Name = "Alyawarr", IsActive = true },
                new Language { Code = "11111812", Name = "Anmatyerr", IsActive = true },
                new Language { Code = "11111813", Name = "Arrernte", IsActive = true },
                new Language { Code = "11111814", Name = "Kaytetye", IsActive = true },
                new Language { Code = "11111815", Name = "Western Arrarnta", IsActive = true },
                new Language { Code = "11112111", Name = "Dhay'yi", IsActive = true },
                new Language { Code = "11112112", Name = "Dhuwal", IsActive = true },
                new Language { Code = "11112113", Name = "Dhuwaya", IsActive = true },
                new Language { Code = "11112114", Name = "Djambarrpuyngu", IsActive = true },
                new Language { Code = "11112115", Name = "Djinang", IsActive = true },
                new Language { Code = "11112116", Name = "Djinba", IsActive = true },
                new Language { Code = "11112117", Name = "Golpa", IsActive = true },
                new Language { Code = "11112118", Name = "Gumatj", IsActive = true },
                new Language { Code = "11112121", Name = "Gupapuyngu", IsActive = true },
                new Language { Code = "11112122", Name = "Ritharrngu", IsActive = true },
                new Language { Code = "11112123", Name = "Yan-nhangu", IsActive = true },
                new Language { Code = "11112124", Name = "Yolngu Matha", IsActive = true },
                new Language { Code = "11112199", Name = "Yolngu Languages, nec", IsActive = true },
                new Language { Code = "11112211", Name = "Kayardild", IsActive = true },
                new Language { Code = "11112212", Name = "Lardil", IsActive = true },
                new Language { Code = "11112213", Name = "Ganggalidda", IsActive = true },
                new Language { Code = "11112311", Name = "Bandjalung", IsActive = true },
                new Language { Code = "11112312", Name = "Yugambeh", IsActive = true },
                new Language { Code = "11112411", Name = "Koko-Bera", IsActive = true },
                new Language { Code = "11112412", Name = "Kugu Nganhcara", IsActive = true },
                new Language { Code = "11112413", Name = "Kuuk Thayorre", IsActive = true },
                new Language { Code = "11112414", Name = "Kuuku-Ya'u", IsActive = true },
                new Language { Code = "11112415", Name = "Morrobalama", IsActive = true },
                new Language { Code = "11112416", Name = "Wik Mungkan", IsActive = true },
                new Language { Code = "11112417", Name = "Wik Ngathan", IsActive = true },
                new Language { Code = "11112418", Name = "Yir Yoront", IsActive = true },
                new Language { Code = "11112499", Name = "Paman Languages, nec", IsActive = true },
                new Language { Code = "11112511", Name = "Guugu Yimidhirr", IsActive = true },
                new Language { Code = "11112512", Name = "Kuku Yalanji", IsActive = true },
                new Language { Code = "11112611", Name = "Djabugay", IsActive = true },
                new Language { Code = "11112612", Name = "Yidiny", IsActive = true },
                new Language { Code = "11112711", Name = "Dyirbal", IsActive = true },
                new Language { Code = "11112712", Name = "Warrgamay", IsActive = true },
                new Language { Code = "11112811", Name = "Biri", IsActive = true },
                new Language { Code = "11112812", Name = "Gudjal", IsActive = true },
                new Language { Code = "11112813", Name = "Gunggari", IsActive = true },
                new Language { Code = "11112814", Name = "Mandandanji", IsActive = true },
                new Language { Code = "11112815", Name = "Yirendali", IsActive = true },
                new Language { Code = "11112899", Name = "Maric Languages, nec", IsActive = true },
                new Language { Code = "11113111", Name = "Butchulla", IsActive = true },
                new Language { Code = "11113112", Name = "Kabi Kabi", IsActive = true },
                new Language { Code = "11113113", Name = "Wakka Wakka", IsActive = true },
                new Language { Code = "11113199", Name = "Waka-Kabic Languages, nec", IsActive = true },
                new Language { Code = "11113211", Name = "Arabana", IsActive = true },
                new Language { Code = "11113212", Name = "Bidjara", IsActive = true },
                new Language { Code = "11113213", Name = "Dieri", IsActive = true },
                new Language { Code = "11113214", Name = "Karuwali", IsActive = true },
                new Language { Code = "11113215", Name = "Kullilli", IsActive = true },
                new Language { Code = "11113216", Name = "Wangkangurru", IsActive = true },
                new Language { Code = "11113299", Name = "Karnic Languages, nec", IsActive = true },
                new Language { Code = "11113311", Name = "Jandai", IsActive = true },
                new Language { Code = "11113312", Name = "Yuggera", IsActive = true },
                new Language { Code = "11113399", Name = "Durubulic Languages, nec", IsActive = true },
                new Language { Code = "11113411", Name = "Gumbaynggir", IsActive = true },
                new Language { Code = "11113412", Name = "Yaygirr", IsActive = true },
                new Language { Code = "11113511", Name = "Bigambul", IsActive = true },
                new Language { Code = "11113512", Name = "Gamilaraay", IsActive = true },
                new Language { Code = "11113513", Name = "Ngiyampaa", IsActive = true },
                new Language { Code = "11113514", Name = "Wiradjuri", IsActive = true },
                new Language { Code = "11113611", Name = "Anaiwan", IsActive = true },
                new Language { Code = "11113612", Name = "Awabakal", IsActive = true },
                new Language { Code = "11113613", Name = "Darkinyung", IsActive = true },
                new Language { Code = "11113614", Name = "Dhanggatti", IsActive = true },
                new Language { Code = "11113615", Name = "Dharawal", IsActive = true },
                new Language { Code = "11113616", Name = "Dharug", IsActive = true },
                new Language { Code = "11113617", Name = "Dhurga", IsActive = true },
                new Language { Code = "11113618", Name = "Gathang (Birrbay/Worimi)", IsActive = true },
                new Language { Code = "11113621", Name = "Gundungurra/Gandangara", IsActive = true },
                new Language { Code = "11113622", Name = "Ngunawal", IsActive = true },
                new Language { Code = "11113699", Name = "Yuin-Kuric Languages, nec", IsActive = true },
                new Language { Code = "11113711", Name = "Djab Wurrung", IsActive = true },
                new Language { Code = "11113712", Name = "Keerray-Woorroong", IsActive = true },
                new Language { Code = "11113713", Name = "Wemba Wemba", IsActive = true },
                new Language { Code = "11113714", Name = "Wergaia", IsActive = true },
                new Language { Code = "11113715", Name = "Woiwurrung", IsActive = true },
                new Language { Code = "11113799", Name = "Kulinic Languages, nec", IsActive = true },
                new Language { Code = "11119911", Name = "Kalaw Lagaw Ya", IsActive = true },
                new Language { Code = "11119912", Name = "Kalkatungu/Kalkadoon", IsActive = true },
                new Language { Code = "11119913", Name = "Kurnai", IsActive = true },
                new Language { Code = "11119914", Name = "Mununjali", IsActive = true },
                new Language { Code = "11119915", Name = "Muruwari", IsActive = true },
                new Language { Code = "11119916", Name = "Ngarrindjeri", IsActive = true },
                new Language { Code = "11119917", Name = "Paakantyi", IsActive = true },
                new Language { Code = "11119918", Name = "Warumungu", IsActive = true },
                new Language { Code = "11119921", Name = "Yanyuwa", IsActive = true },
                new Language { Code = "11119922", Name = "Yorta Yorta", IsActive = true },
                new Language { Code = "11119999", Name = "Other Pama-Nyungan Languages, nec", IsActive = true },
                new Language { Code = "11121111", Name = "Bardi", IsActive = true },
                new Language { Code = "11121112", Name = "Jugun", IsActive = true },
                new Language { Code = "11121113", Name = "Nyikina", IsActive = true },
                new Language { Code = "11121114", Name = "Yawuru", IsActive = true },
                new Language { Code = "11121199", Name = "Nyulnyulan Languages, nec", IsActive = true },
                new Language { Code = "11131111", Name = "Bunuba", IsActive = true },
                new Language { Code = "11131112", Name = "Gooniyandi", IsActive = true },
                new Language { Code = "11141111", Name = "Ngarinyin", IsActive = true },
                new Language { Code = "11141112", Name = "Worrorra", IsActive = true },
                new Language { Code = "11141113", Name = "Wunambal", IsActive = true },
                new Language { Code = "11151111", Name = "Gija", IsActive = true },
                new Language { Code = "11151112", Name = "Miriwoong", IsActive = true },
                new Language { Code = "11161111", Name = "Jaminjung", IsActive = true },
                new Language { Code = "11161112", Name = "Jingulu", IsActive = true },
                new Language { Code = "11161113", Name = "Ngaliwurru", IsActive = true },
                new Language { Code = "11161114", Name = "Nungali", IsActive = true },
                new Language { Code = "11161115", Name = "Wambayan", IsActive = true },
                new Language { Code = "11171111", Name = "Mari Ngarr", IsActive = true },
                new Language { Code = "11171112", Name = "Marramaninyshi", IsActive = true },
                new Language { Code = "11171113", Name = "Marrithiyel", IsActive = true },
                new Language { Code = "11171211", Name = "Murrinh Patha", IsActive = true },
                new Language { Code = "11171212", Name = "Ngan'gi", IsActive = true },
                new Language { Code = "11179911", Name = "Malak Malak", IsActive = true },
                new Language { Code = "11179999", Name = "Other Daly Languages, nec", IsActive = true },
                new Language { Code = "11181111", Name = "Gundjeihmi", IsActive = true },
                new Language { Code = "11181112", Name = "Kune", IsActive = true },
                new Language { Code = "11181113", Name = "Kuninjku", IsActive = true },
                new Language { Code = "11181114", Name = "Kunwinjku", IsActive = true },
                new Language { Code = "11181115", Name = "Mayali", IsActive = true },
                new Language { Code = "11181211", Name = "Anindilyakwa", IsActive = true },
                new Language { Code = "11181212", Name = "Ngandi", IsActive = true },
                new Language { Code = "11181213", Name = "Wubuy", IsActive = true },
                new Language { Code = "11189911", Name = "Dalabon", IsActive = true },
                new Language { Code = "11189912", Name = "Jawoyn", IsActive = true },
                new Language { Code = "11189913", Name = "Kunbarlang", IsActive = true },
                new Language { Code = "11189914", Name = "Ngalakgan", IsActive = true },
                new Language { Code = "11189915", Name = "Rembarrnga", IsActive = true },
                new Language { Code = "11189999", Name = "Other Gunwinyguan Languages, nec", IsActive = true },
                new Language { Code = "11211111", Name = "Amurdak", IsActive = true },
                new Language { Code = "11211112", Name = "Iwaidja", IsActive = true },
                new Language { Code = "11211113", Name = "Maung", IsActive = true },
                new Language { Code = "11221111", Name = "Burarra", IsActive = true },
                new Language { Code = "11221112", Name = "Gurr-goni", IsActive = true },
                new Language { Code = "11221113", Name = "Nakkara", IsActive = true },
                new Language { Code = "11221114", Name = "Ndjébbana (Gunavidji)", IsActive = true },
                new Language { Code = "11231111", Name = "Garrwa", IsActive = true },
                new Language { Code = "11231112", Name = "Waanyi", IsActive = true },
                new Language { Code = "11811111", Name = "Cape York Creole", IsActive = true },
                new Language { Code = "11811112", Name = "Gurindji Kriol", IsActive = true },
                new Language { Code = "11811113", Name = "Kriol", IsActive = true },
                new Language { Code = "11811114", Name = "Lockhart River Creole", IsActive = true },
                new Language { Code = "11811115", Name = "Ngukurr Kriol", IsActive = true },
                new Language { Code = "11811116", Name = "Yumplatok", IsActive = true },
                new Language { Code = "11811117", Name = "Yarrie Lingo", IsActive = true },
                new Language { Code = "11811198", Name = "Aboriginal English, so described", IsActive = true },
                new Language { Code = "11811199", Name = "New and Contact Languages, nec", IsActive = true },
                new Language { Code = "11999911", Name = "Alawa", IsActive = true },
                new Language { Code = "11999912", Name = "Larrakia", IsActive = true },
                new Language { Code = "11999913", Name = "Mangarrayi", IsActive = true },
                new Language { Code = "11999914", Name = "Marra", IsActive = true },
                new Language { Code = "11999915", Name = "Meriam Mir", IsActive = true },
                new Language { Code = "11999916", Name = "palawa kani", IsActive = true },
                new Language { Code = "11999917", Name = "Tiwi", IsActive = true },
                new Language { Code = "11999918", Name = "Wagiman", IsActive = true },
                new Language { Code = "11999921", Name = "Wardaman", IsActive = true },
                new Language { Code = "11999999", Name = "Other Aboriginal and Torres Strait Islander Languages, nec", IsActive = true },
                
                // Creoles
                new Language { Code = "12111111", Name = "Morisyen", IsActive = true },
                new Language { Code = "12111112", Name = "Seychelles Creole", IsActive = true },
                new Language { Code = "12111198", Name = "French creole, so described", IsActive = true },
                new Language { Code = "12111211", Name = "Krio", IsActive = true },
                new Language { Code = "12111212", Name = "Bislama", IsActive = true },
                new Language { Code = "12111213", Name = "Norf'k-Pitcairn", IsActive = true },
                new Language { Code = "12111214", Name = "Pijin", IsActive = true },
                new Language { Code = "12111215", Name = "Tok Pisin (Neomelanesian)", IsActive = true },
                new Language { Code = "12119997", Name = "Portuguese creole, so described", IsActive = true },
                new Language { Code = "12119998", Name = "Spanish creole, so described", IsActive = true },
                new Language { Code = "12119999", Name = "Other Creoles, nec", IsActive = true },
                new Language { Code = "12121111", Name = "Liberian (Liberian English)", IsActive = true },
                new Language { Code = "12121198", Name = "Pidgin, so described", IsActive = true },
                new Language { Code = "12121199", Name = "Pidgins, nec", IsActive = true },
                
                // Indo-European Languages
                new Language { Code = "13111111", Name = "Gaelic (Scotland)", IsActive = true },
                new Language { Code = "13111112", Name = "Irish", IsActive = true },
                new Language { Code = "13111113", Name = "Welsh", IsActive = true },
                new Language { Code = "13111199", Name = "Insular Languages, nec", IsActive = true },
                new Language { Code = "13121111", Name = "Afrikaans", IsActive = true },
                new Language { Code = "13121112", Name = "Dutch", IsActive = true },
                new Language { Code = "13121113", Name = "English", IsActive = true },
                new Language { Code = "13121114", Name = "Frisian", IsActive = true },
                new Language { Code = "13121115", Name = "German", IsActive = true },
                new Language { Code = "13121116", Name = "Luxembourgish", IsActive = true },
                new Language { Code = "13121117", Name = "Yiddish", IsActive = true },
                new Language { Code = "13121199", Name = "West Germanic Languages, nec", IsActive = true },
                new Language { Code = "13121211", Name = "Danish", IsActive = true },
                new Language { Code = "13121212", Name = "Icelandic", IsActive = true },
                new Language { Code = "13121213", Name = "Norwegian", IsActive = true },
                new Language { Code = "13121214", Name = "Swedish", IsActive = true },
                new Language { Code = "13121299", Name = "North Germanic Languages, nec", IsActive = true },
                new Language { Code = "13131111", Name = "Aromanian", IsActive = true },
                new Language { Code = "13131112", Name = "Catalan", IsActive = true },
                new Language { Code = "13131113", Name = "French", IsActive = true },
                new Language { Code = "13131114", Name = "Italian", IsActive = true },
                new Language { Code = "13131115", Name = "Portuguese", IsActive = true },
                new Language { Code = "13131116", Name = "Romanian", IsActive = true },
                new Language { Code = "13131117", Name = "Spanish", IsActive = true },
                new Language { Code = "13139911", Name = "Latin", IsActive = true },
                new Language { Code = "13139999", Name = "Other Italic Languages, nec", IsActive = true },
                new Language { Code = "13141111", Name = "Greek", IsActive = true },
                new Language { Code = "13141199", Name = "Greek Languages, nec", IsActive = true },
                new Language { Code = "13151111", Name = "Latvian", IsActive = true },
                new Language { Code = "13151112", Name = "Lithuanian", IsActive = true },
                new Language { Code = "13151211", Name = "Belorussian", IsActive = true },
                new Language { Code = "13151212", Name = "Bosnian", IsActive = true },
                new Language { Code = "13151213", Name = "Bulgarian", IsActive = true },
                new Language { Code = "13151214", Name = "Croatian", IsActive = true },
                new Language { Code = "13151215", Name = "Czech", IsActive = true },
                new Language { Code = "13151216", Name = "Macedonian", IsActive = true },
                new Language { Code = "13151217", Name = "Polish", IsActive = true },
                new Language { Code = "13151218", Name = "Russian", IsActive = true },
                new Language { Code = "13151221", Name = "Serbian", IsActive = true },
                new Language { Code = "13151222", Name = "Slovak", IsActive = true },
                new Language { Code = "13151223", Name = "Slovene", IsActive = true },
                new Language { Code = "13151224", Name = "Ukrainian", IsActive = true },
                new Language { Code = "13159997", Name = "Czechoslovakian, so described", IsActive = true },
                new Language { Code = "13159998", Name = "Serbo-Croatian/Yugoslavian, so described", IsActive = true },
                new Language { Code = "13159999", Name = "Other Balto-Slavic Languages, nec", IsActive = true },
                new Language { Code = "13161111", Name = "Balochi", IsActive = true },
                new Language { Code = "13161112", Name = "Dari", IsActive = true },
                new Language { Code = "13161113", Name = "Hazaragi", IsActive = true },
                new Language { Code = "13161114", Name = "Northern Kurdish (Kurmanji)", IsActive = true },
                new Language { Code = "13161115", Name = "Pashto", IsActive = true },
                new Language { Code = "13161116", Name = "Persian (excluding Dari)", IsActive = true },
                new Language { Code = "13161211", Name = "Assamese", IsActive = true },
                new Language { Code = "13161212", Name = "Bengali", IsActive = true },
                new Language { Code = "13161213", Name = "Dhivehi", IsActive = true },
                new Language { Code = "13161214", Name = "Fiji Hindi", IsActive = true },
                new Language { Code = "13161215", Name = "Gujarati", IsActive = true },
                new Language { Code = "13161216", Name = "Haryanvi", IsActive = true },
                new Language { Code = "13161217", Name = "Hindi", IsActive = true },
                new Language { Code = "13161218", Name = "Kashmiri", IsActive = true },
                new Language { Code = "13161221", Name = "Konkani", IsActive = true },
                new Language { Code = "13161222", Name = "Marathi", IsActive = true },
                new Language { Code = "13161223", Name = "Nepali", IsActive = true },
                new Language { Code = "13161224", Name = "Oriya", IsActive = true },
                new Language { Code = "13161225", Name = "Punjabi", IsActive = true },
                new Language { Code = "13161226", Name = "Rohingya", IsActive = true },
                new Language { Code = "13161227", Name = "Romany", IsActive = true },
                new Language { Code = "13161228", Name = "Sindhi", IsActive = true },
                new Language { Code = "13161231", Name = "Sinhalese", IsActive = true },
                new Language { Code = "13161232", Name = "Urdu", IsActive = true },
                new Language { Code = "13169999", Name = "Other Indo-Iranian Languages, nec", IsActive = true },
                new Language { Code = "13999911", Name = "Albanian", IsActive = true },
                new Language { Code = "13999912", Name = "Armenian", IsActive = true },
                new Language { Code = "13999998", Name = "Swiss, so described", IsActive = true },
                new Language { Code = "13999999", Name = "Other Indo-European Languages, nec", IsActive = true },
                
                // Uralic Languages
                new Language { Code = "14111111", Name = "Estonian", IsActive = true },
                new Language { Code = "14111112", Name = "Finnish", IsActive = true },
                new Language { Code = "14999911", Name = "Hungarian", IsActive = true },
                new Language { Code = "14999999", Name = "Other Uralic Languages, nec", IsActive = true },
                
                // Afro-Asiatic Languages
                new Language { Code = "15111111", Name = "Arabic", IsActive = true },
                new Language { Code = "15111112", Name = "Assyrian Neo-Aramaic", IsActive = true },
                new Language { Code = "15111113", Name = "Chaldean Neo-Aramaic", IsActive = true },
                new Language { Code = "15111114", Name = "Hebrew", IsActive = true },
                new Language { Code = "15111115", Name = "Maltese", IsActive = true },
                new Language { Code = "15111116", Name = "Mandaean (Mandaic)", IsActive = true },
                new Language { Code = "15111117", Name = "Syriac", IsActive = true },
                new Language { Code = "15111199", Name = "Central Semitic Languages, nec", IsActive = true },
                new Language { Code = "15111211", Name = "Amharic", IsActive = true },
                new Language { Code = "15111212", Name = "Harari", IsActive = true },
                new Language { Code = "15111213", Name = "Tigré", IsActive = true },
                new Language { Code = "15111214", Name = "Tigrinya", IsActive = true },
                new Language { Code = "15111299", Name = "South Semitic Languages, nec", IsActive = true },
                new Language { Code = "15121111", Name = "Oromo", IsActive = true },
                new Language { Code = "15121112", Name = "Somali", IsActive = true },
                new Language { Code = "15129999", Name = "Other Cushitic Languages, nec", IsActive = true },
                new Language { Code = "15999911", Name = "Hausa", IsActive = true },
                new Language { Code = "15999999", Name = "Other Afro-Asiatic Languages, nec", IsActive = true },
                
                // Turkic Languages
                new Language { Code = "16111111", Name = "Azeri", IsActive = true },
                new Language { Code = "16111112", Name = "Turkish", IsActive = true },
                new Language { Code = "16111113", Name = "Turkmen", IsActive = true },
                new Language { Code = "16121111", Name = "Uyghur", IsActive = true },
                new Language { Code = "16121112", Name = "Uzbek", IsActive = true },
                new Language { Code = "16999911", Name = "Tatar", IsActive = true },
                new Language { Code = "16999999", Name = "Other Turkic Languages, nec", IsActive = true },
                
                // Niger-Congo Languages
                new Language { Code = "17111111", Name = "Akan", IsActive = true },
                new Language { Code = "17111112", Name = "Bassa", IsActive = true },
                new Language { Code = "17111113", Name = "Bemba", IsActive = true },
                new Language { Code = "17111114", Name = "Edo", IsActive = true },
                new Language { Code = "17111115", Name = "Ewe", IsActive = true },
                new Language { Code = "17111116", Name = "Ga", IsActive = true },
                new Language { Code = "17111117", Name = "Igbo", IsActive = true },
                new Language { Code = "17111118", Name = "Kikuyu", IsActive = true },
                new Language { Code = "17111121", Name = "Kinyarwanda (Rwanda)", IsActive = true },
                new Language { Code = "17111122", Name = "Kirundi (Rundi)", IsActive = true },
                new Language { Code = "17111123", Name = "Krahn", IsActive = true },
                new Language { Code = "17111124", Name = "Lingala", IsActive = true },
                new Language { Code = "17111125", Name = "Luganda", IsActive = true },
                new Language { Code = "17111126", Name = "Ndebele", IsActive = true },
                new Language { Code = "17111127", Name = "Nyanja (Chichewa)", IsActive = true },
                new Language { Code = "17111128", Name = "Shona", IsActive = true },
                new Language { Code = "17111131", Name = "Swahili", IsActive = true },
                new Language { Code = "17111132", Name = "Tswana", IsActive = true },
                new Language { Code = "17111133", Name = "Xhosa", IsActive = true },
                new Language { Code = "17111134", Name = "Yoruba", IsActive = true },
                new Language { Code = "17111135", Name = "Zulu", IsActive = true },
                new Language { Code = "17119911", Name = "Fulfulde", IsActive = true },
                new Language { Code = "17119912", Name = "Kissi", IsActive = true },
                new Language { Code = "17119999", Name = "Other Atlantic Congo Languages, nec", IsActive = true },
                new Language { Code = "17121111", Name = "Kpelle", IsActive = true },
                new Language { Code = "17121112", Name = "Loma (Lorma)", IsActive = true },
                new Language { Code = "17121113", Name = "Mandinka", IsActive = true },
                new Language { Code = "17121114", Name = "Mende", IsActive = true },
                new Language { Code = "17121199", Name = "Western Mande Languages, nec", IsActive = true },
                new Language { Code = "17121211", Name = "Dan (Gio-Dan)", IsActive = true },
                new Language { Code = "17121212", Name = "Maan", IsActive = true },
                new Language { Code = "17121299", Name = "Eastern Mande Languages, nec", IsActive = true },
                new Language { Code = "17999911", Name = "Moro (Nuba Moro)", IsActive = true },
                new Language { Code = "17999999", Name = "Other Niger Congo Languages, nec", IsActive = true },
                
                // Nilo-Saharan Languages
                new Language { Code = "18111111", Name = "Acholi", IsActive = true },
                new Language { Code = "18111112", Name = "Anuak", IsActive = true },
                new Language { Code = "18111113", Name = "Bari", IsActive = true },
                new Language { Code = "18111114", Name = "Dinka", IsActive = true },
                new Language { Code = "18111115", Name = "Luo", IsActive = true },
                new Language { Code = "18111116", Name = "Madi", IsActive = true },
                new Language { Code = "18111117", Name = "Nuer", IsActive = true },
                new Language { Code = "18111118", Name = "Shilluk", IsActive = true },
                new Language { Code = "18999999", Name = "Other Nilo-Saharan Languages, nec", IsActive = true },
                
                // Dravidian Languages
                new Language { Code = "21111111", Name = "Kannada", IsActive = true },
                new Language { Code = "21111112", Name = "Malayalam", IsActive = true },
                new Language { Code = "21111113", Name = "Tamil", IsActive = true },
                new Language { Code = "21111114", Name = "Tulu", IsActive = true },
                new Language { Code = "21999911", Name = "Telugu", IsActive = true },
                new Language { Code = "21999999", Name = "Other Dravidian Languages, nec", IsActive = true },
                
                // Other Language Families
                new Language { Code = "22111111", Name = "Basque", IsActive = true },
                new Language { Code = "22111112", Name = "Georgian", IsActive = true },
                new Language { Code = "22111113", Name = "Japanese", IsActive = true },
                new Language { Code = "22111114", Name = "Korean", IsActive = true },
                new Language { Code = "22111115", Name = "Mongolian", IsActive = true },
                
                // Sino-Tibetan Languages
                new Language { Code = "23111111", Name = "Hakka", IsActive = true },
                new Language { Code = "23111112", Name = "Mandarin", IsActive = true },
                new Language { Code = "23111113", Name = "Min Nan (Teochew)", IsActive = true },
                new Language { Code = "23111114", Name = "Wu (Shanghainese)", IsActive = true },
                new Language { Code = "23111115", Name = "Yue (Cantonese)", IsActive = true },
                new Language { Code = "23111199", Name = "Chinese Languages, nec", IsActive = true },
                new Language { Code = "23121111", Name = "Falam Chin", IsActive = true },
                new Language { Code = "23121112", Name = "Hakha Chin", IsActive = true },
                new Language { Code = "23121113", Name = "Kachin", IsActive = true },
                new Language { Code = "23121114", Name = "Matu Chin", IsActive = true },
                new Language { Code = "23121115", Name = "Mizo", IsActive = true },
                new Language { Code = "23121116", Name = "Siyin Chin", IsActive = true },
                new Language { Code = "23121117", Name = "Tedim Chin", IsActive = true },
                new Language { Code = "23121118", Name = "Zophei", IsActive = true },
                new Language { Code = "23121121", Name = "Zotung Chin", IsActive = true },
                new Language { Code = "23121211", Name = "Karenni", IsActive = true },
                new Language { Code = "23121212", Name = "Pwo Karen", IsActive = true },
                new Language { Code = "23121213", Name = "S'gaw Karen", IsActive = true },
                new Language { Code = "23121311", Name = "Dzongkha", IsActive = true },
                new Language { Code = "23121312", Name = "Central Tibetan", IsActive = true },
                new Language { Code = "23129911", Name = "Burmese", IsActive = true },
                new Language { Code = "23129999", Name = "Other Tibeto-Burman Languages, nec", IsActive = true },
                
                // Hmong-Mien
                new Language { Code = "24111111", Name = "Hmong", IsActive = true },
                new Language { Code = "24111199", Name = "Hmong-Mien Languages, nec", IsActive = true },
                
                // Austro-Asiatic
                new Language { Code = "25111111", Name = "Khmer", IsActive = true },
                new Language { Code = "25111112", Name = "Mon", IsActive = true },
                new Language { Code = "25111113", Name = "Vietnamese", IsActive = true },
                new Language { Code = "25111199", Name = "Austro-Asiatic Languages, nec", IsActive = true },
                
                // Kra-Dai
                new Language { Code = "26111111", Name = "Lao", IsActive = true },
                new Language { Code = "26111112", Name = "Thai", IsActive = true },
                new Language { Code = "26119999", Name = "Other Kra-Dai Languages, nec", IsActive = true },
                
                // Austronesian
                new Language { Code = "27111111", Name = "Bikol", IsActive = true },
                new Language { Code = "27111112", Name = "Bisaya", IsActive = true },
                new Language { Code = "27111113", Name = "Cebuano", IsActive = true },
                new Language { Code = "27111114", Name = "Filipino", IsActive = true },
                new Language { Code = "27111115", Name = "Tagalog", IsActive = true },
                new Language { Code = "27111211", Name = "IIocano", IsActive = true },
                new Language { Code = "27111212", Name = "Ilonggo (Hiligaynon)", IsActive = true },
                new Language { Code = "27111311", Name = "Acehnese", IsActive = true },
                new Language { Code = "27111312", Name = "Iban", IsActive = true },
                new Language { Code = "27111313", Name = "Indonesian", IsActive = true },
                new Language { Code = "27111314", Name = "Malay", IsActive = true },
                new Language { Code = "27111411", Name = "Fijian", IsActive = true },
                new Language { Code = "27111412", Name = "Kiribati", IsActive = true },
                new Language { Code = "27111413", Name = "Cook Islands Maori", IsActive = true },
                new Language { Code = "27111414", Name = "M?ori (New Zealand)", IsActive = true },
                new Language { Code = "27111415", Name = "Meto", IsActive = true },
                new Language { Code = "27111416", Name = "Motu (Hiri Motu)", IsActive = true },
                new Language { Code = "27111417", Name = "Nauruan", IsActive = true },
                new Language { Code = "27111418", Name = "Niue", IsActive = true },
                new Language { Code = "27111421", Name = "Rotuman", IsActive = true },
                new Language { Code = "27111422", Name = "Samoan", IsActive = true },
                new Language { Code = "27111423", Name = "Tetun", IsActive = true },
                new Language { Code = "27111424", Name = "Tokelauan", IsActive = true },
                new Language { Code = "27111425", Name = "Tongan", IsActive = true },
                new Language { Code = "27111426", Name = "Tuvaluan", IsActive = true },
                new Language { Code = "27119911", Name = "Balinese", IsActive = true },
                new Language { Code = "27119912", Name = "Javanese", IsActive = true },
                new Language { Code = "27119913", Name = "Pampangan", IsActive = true },
                new Language { Code = "27119999", Name = "Other Malayo-Polynesian Languages, nec", IsActive = true },
                new Language { Code = "27999999", Name = "Other Austronesian Languages, nec", IsActive = true },
                
                // Miscellaneous
                new Language { Code = "91111111", Name = "American Languages", IsActive = true },
                new Language { Code = "91111198", Name = "Cypriot, so described", IsActive = true },
                new Language { Code = "91111199", Name = "Other Languages, nec", IsActive = true },
                new Language { Code = "91711111", Name = "Auslan", IsActive = true },
                new Language { Code = "91711112", Name = "Key Word Sign Australia", IsActive = true },
                new Language { Code = "91711199", Name = "Sign Languages, nec", IsActive = true },
                new Language { Code = "91981111", Name = "Invented Languages", IsActive = true },
                new Language { Code = "91991111", Name = "Non verbal, so described", IsActive = true }
            };

            await context.Languages.AddRangeAsync(languages);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} languages successfully", languages.Count);
        }

        private static async Task SeedContactClassificationsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.ContactClassifications.AnyAsync())
            {
                logger.LogInformation("ContactClassifications table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Contact Classifications...");

            var contactClassifications = new List<ContactClassification>
            {
                new ContactClassification { Name = "Household Contact", Description = "Lives in the same household as the case", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ContactClassification { Name = "Sexual Contact", Description = "Sexual partner of the case", DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ContactClassification { Name = "Close Proximity Contact", Description = "Face-to-face contact within 1.5 meters for 15 minutes or more", DisplayOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ContactClassification { Name = "Healthcare Contact", Description = "Healthcare worker providing direct care without appropriate PPE", DisplayOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ContactClassification { Name = "Social Contact", Description = "Social gathering or event attendance", DisplayOrder = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ContactClassification { Name = "Workplace Contact", Description = "Contact at workplace", DisplayOrder = 6, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ContactClassification { Name = "School/Education Contact", Description = "Contact at school or educational facility", DisplayOrder = 7, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ContactClassification { Name = "Unknown/Other", Description = "Contact classification unknown or not specified", DisplayOrder = 8, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            await context.ContactClassifications.AddRangeAsync(contactClassifications);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} contact classifications successfully", contactClassifications.Count);
        }

        private static async Task SeedAtsiStatusesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.AtsiStatuses.AnyAsync())
            {
                logger.LogInformation("ATSI Statuses table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding ATSI Statuses...");

            var atsiStatuses = new List<AboriginalTorresStraitIslanderStatus>
            {
                new AboriginalTorresStraitIslanderStatus { Name = "Aboriginal", IsActive = true },
                new AboriginalTorresStraitIslanderStatus { Name = "Torres Strait Islander", IsActive = true },
                new AboriginalTorresStraitIslanderStatus { Name = "Aboriginal and Torres Strait Islander", IsActive = true },
                new AboriginalTorresStraitIslanderStatus { Name = "Neither", IsActive = true }
            };

            await context.AtsiStatuses.AddRangeAsync(atsiStatuses);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} ATSI statuses successfully", atsiStatuses.Count);
        }

        private static async Task SeedSexAtBirthAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.SexAtBirths.AnyAsync())
            {
                logger.LogInformation("SexAtBirth table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Sex At Birth...");

            var sexAtBirths = new List<SexAtBirth>
            {
                new SexAtBirth { Name = "Male", IsActive = true },
                new SexAtBirth { Name = "Female", IsActive = true },
                new SexAtBirth { Name = "Another Term", IsActive = true }
            };

            await context.SexAtBirths.AddRangeAsync(sexAtBirths);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} sex at birth options successfully", sexAtBirths.Count);
        }

        private static async Task SeedGendersAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Genders.AnyAsync())
            {
                logger.LogInformation("Genders table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Genders...");

            var genders = new List<Gender>
            {
                new Gender { Name = "Man or male", IsActive = true },
                new Gender { Name = "Woman or female", IsActive = true },
                new Gender { Name = "Non-binary", IsActive = true },
                new Gender { Name = "[I/They] use a different term (please specify)", IsActive = true },
                new Gender { Name = "Prefer not to answer", IsActive = true }
            };

            await context.Genders.AddRangeAsync(genders);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} genders successfully", genders.Count);
        }

        private static async Task SeedCaseStatusesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.CaseStatuses.AnyAsync())
            {
                logger.LogInformation("CaseStatuses table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Case Statuses...");

            var caseStatuses = new List<CaseStatus>
            {
                new CaseStatus { Name = "Confirmed", IsActive = true },
                new CaseStatus { Name = "Probable", IsActive = true },
                new CaseStatus { Name = "Presumptive", IsActive = true },
                new CaseStatus { Name = "Rejected", IsActive = true },
                new CaseStatus { Name = "Under Investigation", IsActive = true },
                new CaseStatus { Name = "Closed", IsActive = true },
                new CaseStatus { Name = "Completed", IsActive = true }
            };

            await context.CaseStatuses.AddRangeAsync(caseStatuses);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} case statuses successfully", caseStatuses.Count);
        }

        private static async Task SeedDiseaseCategoriesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.DiseaseCategories.AnyAsync())
            {
                logger.LogInformation("DiseaseCategories table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Disease Categories...");

            var diseaseCategories = new List<DiseaseCategory>
            {
                new DiseaseCategory { Name = "Vaccine Preventable Diseases", ReportingId = "VPD", Description = "Diseases that can be prevented through vaccination", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new DiseaseCategory { Name = "Vector Borne Diseases", ReportingId = "VBD", Description = "Diseases transmitted by vectors such as mosquitoes and ticks", DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new DiseaseCategory { Name = "Foodborne Diseases", ReportingId = "FBD", Description = "Diseases acquired through contaminated food or water", DisplayOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                new DiseaseCategory { Name = "Sexually Transmitted Infections", ReportingId = "STI", Description = "Infections transmitted through sexual contact", DisplayOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
                new DiseaseCategory { Name = "Airborne Diseases", ReportingId = "ABD", Description = "Diseases transmitted through airborne particles", DisplayOrder = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
                new DiseaseCategory { Name = "Zoonotic Diseases", ReportingId = "ZOO", Description = "Diseases transmitted from animals to humans", DisplayOrder = 6, IsActive = true, CreatedAt = DateTime.UtcNow },
                new DiseaseCategory { Name = "Antimicrobial Resistant Organisms", ReportingId = "AMR", Description = "Drug-resistant bacterial infections", DisplayOrder = 7, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            await context.DiseaseCategories.AddRangeAsync(diseaseCategories);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} disease categories successfully", diseaseCategories.Count);
        }

        private static async Task SeedOrganizationTypesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.OrganizationTypes.AnyAsync())
            {
                logger.LogInformation("OrganizationTypes table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Organization Types...");

            var organizationTypes = new List<OrganizationType>
            {
                new OrganizationType { Name = "Hospital", Description = "Hospital or medical center", IsActive = true },
                new OrganizationType { Name = "Laboratory", Description = "Pathology or diagnostic laboratory", IsActive = true },
                new OrganizationType { Name = "Medical Practitioner", Description = "Individual medical practitioner", IsActive = true },
                new OrganizationType { Name = "Medical Clinic", Description = "General practice or medical clinic", IsActive = true },
                new OrganizationType { Name = "Pharmacy", Description = "Pharmacy or dispensary", IsActive = true },
                new OrganizationType { Name = "School", Description = "School or educational institution", IsActive = true },
                new OrganizationType { Name = "Childcare Centre", Description = "Childcare or early learning centre", IsActive = true }
            };

            await context.OrganizationTypes.AddRangeAsync(organizationTypes);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} organization types successfully", organizationTypes.Count);
        }

        private static async Task SeedSpecimenTypesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.SpecimenTypes.AnyAsync())
            {
                logger.LogInformation("SpecimenTypes table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Specimen Types...");

            var specimenTypes = new List<SpecimenType>
            {
                new SpecimenType { Name = "Blood", Description = "Whole blood specimen", IsActive = true },
                new SpecimenType { Name = "Urine", Description = "Urine specimen", IsActive = true },
                new SpecimenType { Name = "Nasopharyngeal Swab", Description = "Swab from nasopharynx", IsActive = true },
                new SpecimenType { Name = "CSF Fluid", Description = "Cerebrospinal fluid", IsActive = true },
                new SpecimenType { Name = "Swab (other)", Description = "Other swab specimen", IsActive = true },
                new SpecimenType { Name = "Penile Swab", Description = "Swab from penile site", IsActive = true },
                new SpecimenType { Name = "Urethral Swab", Description = "Swab from urethra", IsActive = true },
                new SpecimenType { Name = "Vaginal Swab", Description = "Swab from vaginal site", IsActive = true },
                new SpecimenType { Name = "Cervical Swab", Description = "Swab from cervical site", IsActive = true }
            };

            await context.SpecimenTypes.AddRangeAsync(specimenTypes);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} specimen types successfully", specimenTypes.Count);
        }

        private static async Task SeedTestTypesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.TestTypes.AnyAsync())
            {
                logger.LogInformation("TestTypes table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Test Types...");

            var testTypes = new List<TestType>
            {
                new TestType { Name = "PCR", Description = "Polymerase Chain Reaction", IsActive = true },
                new TestType { Name = "Culture", Description = "Bacterial or viral culture", IsActive = true },
                new TestType { Name = "Serology", Description = "Antibody detection test", IsActive = true },
                new TestType { Name = "Microscopy", Description = "Microscopic examination", IsActive = true }
            };

            await context.TestTypes.AddRangeAsync(testTypes);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} test types successfully", testTypes.Count);
        }

        private static async Task SeedTestResultsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.TestResults.AnyAsync())
            {
                logger.LogInformation("TestResults table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Test Results...");

            var testResults = new List<TestResult>
            {
                new TestResult { Name = "Detected", Description = "PCR - Pathogen detected", IsActive = true },
                new TestResult { Name = "Not Detected", Description = "PCR - Pathogen not detected", IsActive = true },
                new TestResult { Name = "Equivocal", Description = "PCR - Inconclusive result", IsActive = true },
                new TestResult { Name = "No Growth", Description = "Culture - No bacterial/viral growth", IsActive = true },
                new TestResult { Name = "Growth Of", Description = "Culture - Growth of organism (specify)", IsActive = true },
                new TestResult { Name = "IgG Positive", Description = "Serology - IgG antibodies detected", IsActive = true },
                new TestResult { Name = "IgM Positive", Description = "Serology - IgM antibodies detected", IsActive = true },
                new TestResult { Name = "IgG Negative", Description = "Serology - IgG antibodies not detected", IsActive = true },
                new TestResult { Name = "IgM Negative", Description = "Serology - IgM antibodies not detected", IsActive = true }
            };

            await context.TestResults.AddRangeAsync(testResults);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} test results successfully", testResults.Count);
        }

        private static async Task SeedSymptomsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.Symptoms.AnyAsync())
            {
                logger.LogInformation("Symptoms table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Symptoms...");

            var symptoms = new List<Symptom>
            {
                new Symptom { Name = "Fever", Description = "Elevated body temperature >38°C", SortOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Cough", Description = "Persistent cough", SortOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Shortness of Breath", Description = "Difficulty breathing or dyspnea", SortOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Fatigue", Description = "Extreme tiredness or exhaustion", SortOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Headache", Description = "Pain in the head or neck region", SortOrder = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Sore Throat", Description = "Pain or irritation in the throat", SortOrder = 6, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Muscle Aches", Description = "Myalgia or body aches", SortOrder = 7, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Chills", Description = "Feeling of coldness with shivering", SortOrder = 8, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Nausea", Description = "Feeling of sickness with urge to vomit", SortOrder = 9, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Vomiting", Description = "Forceful expulsion of stomach contents", SortOrder = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Diarrhea", Description = "Loose or watery stools", SortOrder = 11, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Loss of Taste", Description = "Ageusia or reduced taste sensation", SortOrder = 12, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Loss of Smell", Description = "Anosmia or reduced smell sensation", SortOrder = 13, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Rash", Description = "Skin eruption or redness", SortOrder = 14, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Chest Pain", Description = "Pain or discomfort in chest area", SortOrder = 15, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Confusion", Description = "Mental confusion or altered consciousness", SortOrder = 16, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Seizures", Description = "Convulsions or fits", SortOrder = 17, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Joint Pain", Description = "Arthralgia or joint discomfort", SortOrder = 18, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Abdominal Pain", Description = "Pain in the abdominal region", SortOrder = 19, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Runny Nose", Description = "Nasal discharge or rhinorrhea", SortOrder = 20, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Sneezing", Description = "Involuntary expulsion of air from nose", SortOrder = 21, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Watery Eyes", Description = "Excessive tearing or lacrimation", SortOrder = 22, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Swollen Lymph Nodes", Description = "Enlarged lymph glands", SortOrder = 23, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Night Sweats", Description = "Excessive sweating during sleep", SortOrder = 24, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Symptom { Name = "Weight Loss", Description = "Unintentional loss of body weight", SortOrder = 25, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            await context.Symptoms.AddRangeAsync(symptoms);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} symptoms successfully", symptoms.Count);
        }

        private static async Task SeedTaskTypesAsync(ApplicationDbContext context, ILogger logger)
        {
            if (await context.TaskTypes.AnyAsync())
            {
                logger.LogInformation("TaskTypes table already has data - skipping seeding");
                return;
            }

            logger.LogInformation("Seeding Task Types...");

            var taskTypes = new List<TaskType>
            {
                new TaskType { Name = "Survey", Description = "Conduct case or contact survey", IsActive = true },
                new TaskType { Name = "Case Interview", Description = "Initial case interview and investigation", IsActive = true },
                new TaskType { Name = "Isolation", Description = "Arrange or monitor isolation requirements", IsActive = true },
                new TaskType { Name = "Medication Advice", Description = "Provide medication or treatment advice", IsActive = true },
                new TaskType { Name = "Quarantine", Description = "Arrange or monitor quarantine requirements", IsActive = true }
            };

            await context.TaskTypes.AddRangeAsync(taskTypes);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} task types successfully", taskTypes.Count);
        }
    }
}
