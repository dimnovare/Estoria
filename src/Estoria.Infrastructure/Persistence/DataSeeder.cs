using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Infrastructure.Persistence;

public class DataSeeder
{
    private readonly AppDbContext _db;

    public DataSeeder(AppDbContext db) => _db = db;

    public async Task SeedAsync()
    {
        // SiteSettings have their own per-key idempotent guards so they get
        // populated on existing DBs too — must run before the homepage.hero gate.
        await SeedSiteSettingsAsync();

        // Guard: use a single lightweight check — if the first seed key already exists, skip all
        if (await _db.PageContents.AnyAsync(p => p.PageKey == "homepage.hero"))
            return;

        // ── 1. Page Content ───────────────────────────────────────────────────

        var pages = new List<PageContent>
        {
            PageContent("homepage.hero", new (Language, string?, string?, string?, string?)[]
            {
                (Language.En, "Where Your Future Lives",
                    "Discover premium properties across Tallinn — from vibrant city-centre apartments to peaceful suburban homes.",
                    "https://placehold.co/1920x1080", null),
                (Language.Et, "Kus Sinu tulevik elab",
                    "Avasta esmaklassilisi kinnisvara üle Tallinna — elavatest kesklinna korteritest rahulike äärelinna kodudeni.",
                    "https://placehold.co/1920x1080", null),
                (Language.Ru, "Где живёт ваше будущее",
                    "Откройте для себя элитную недвижимость по всему Таллину — от оживлённых квартир в центре города до тихих загородных домов.",
                    "https://placehold.co/1920x1080", null),
            }),

            PageContent("homepage.featured", new (Language, string?, string?, string?, string?)[]
            {
                (Language.En, "Featured Properties",
                    "Hand-picked listings that stand out for their location, design and value.",
                    null, null),
                (Language.Et, "Esiletõstetud kinnisvara",
                    "Käsitsi valitud kuulutused, mis paistavad silma asukoha, disaini ja väärtuse poolest.",
                    null, null),
                (Language.Ru, "Избранная недвижимость",
                    "Тщательно отобранные объявления, выделяющиеся своим расположением, дизайном и стоимостью.",
                    null, null),
            }),

            PageContent("homepage.services", new (Language, string?, string?, string?, string?)[]
            {
                (Language.En, "Our Services",
                    "Full-service real estate agency offering sales, rentals, valuations and legal consulting.",
                    null, null),
                (Language.Et, "Meie teenused",
                    "Täisteenusega kinnisvarabüroo, mis pakub müüki, üürimist, hindamist ja juriidilist nõustamist.",
                    null, null),
                (Language.Ru, "Наши услуги",
                    "Агентство недвижимости полного цикла, предлагающее продажу, аренду, оценку и юридические консультации.",
                    null, null),
            }),

            PageContent("homepage.cta", new (Language, string?, string?, string?, string?)[]
            {
                (Language.En, "Ready to find your home?",
                    "Our experienced team is here to guide you through every step of buying, selling or renting a property in Estonia.",
                    null, null),
                (Language.Et, "Valmis oma kodu leidma?",
                    "Meie kogenud meeskond on siin, et juhtida teid läbi iga sammu kinnisvara ostmisel, müümisel või üürimisel Eestis.",
                    null, null),
                (Language.Ru, "Готовы найти свой дом?",
                    "Наша опытная команда готова провести вас через каждый шаг покупки, продажи или аренды недвижимости в Эстонии.",
                    null, null),
            }),

            PageContent("about.intro", new (Language, string?, string?, string?, string?)[]
            {
                (Language.En, "About Estoria",
                    "Estoria is Tallinn's trusted real estate partner. Founded in 2010, we have helped over 3 000 families find their perfect home. Our multilingual team of certified agents combines deep local knowledge with a personalised approach — ensuring every client receives honest advice and outstanding service.",
                    "https://placehold.co/1200x800", null),
                (Language.Et, "Estoriast",
                    "Estoria on Tallinna usaldusväärne kinnisvarapartner. Asutatud 2010. aastal, oleme aidanud üle 3 000 pere leida oma täiusliku kodu. Meie mitmekeelne sertifitseeritud maaklerite meeskond ühendab sügavad kohalikud teadmised personaalse lähenemisega — tagades, et iga klient saab ausaid nõuandeid ja suurepärase teeninduse.",
                    "https://placehold.co/1200x800", null),
                (Language.Ru, "О нас",
                    "Estoria — надёжный партнёр в сфере недвижимости в Таллине. Основанная в 2010 году, наша компания помогла более чем 3 000 семьям найти идеальный дом. Наша многоязычная команда сертифицированных агентов сочетает глубокое знание местного рынка с персональным подходом — гарантируя каждому клиенту честные рекомендации и исключительный сервис.",
                    "https://placehold.co/1200x800", null),
            }),

            PageContent("about.story", new (Language, string?, string?, string?, string?)[]
            {
                (Language.En, "Our Story",
                    "Estoria began as a small family business in Kalamaja, back when the neighbourhood was still a hidden gem. We watched Tallinn grow and evolve — and grew with it. From our first one-room office on Kotzebue Street, we have expanded to a full-service agency with specialists in residential, commercial and investment property.\n\nOver the years we have built a reputation for integrity, transparent pricing and honest communication. We do not just close deals — we build lasting relationships with our clients and communities.",
                    null, null),
                (Language.Et, "Meie lugu",
                    "Estoria sai alguse väikese pereettevõttena Kalamajas, kui see linnaosa oli veel avastamata pärl. Nägime, kuidas Tallinn kasvas ja arenes — ja kasvasime koos sellega. Meie esimesest ühetoalisest kontorist Kotzebue tänaval oleme laienenud täisteenuseid pakkuvaks agentuuriks, kus on spetsialistid elamu-, äri- ja investeerimiskinnisvara valdkonnas.\n\nAastate jooksul oleme loonud maine aususe, läbipaistvate hindade ja ausa suhtluse poolest. Me ei sulge lihtsalt tehinguid — me loome püsivaid suhteid klientide ja kogukondadega.",
                    null, null),
                (Language.Ru, "Наша история",
                    "Estoria начиналась как небольшой семейный бизнес в Каламая, когда этот район ещё был скрытой жемчужиной. Мы наблюдали, как Таллин рос и развивался — и росли вместе с ним. Из нашего первого однокомнатного офиса на улице Котцебуэ мы превратились в агентство полного цикла со специалистами по жилой, коммерческой и инвестиционной недвижимости.\n\nСо временем мы создали репутацию, основанную на честности, прозрачном ценообразовании и открытом общении. Мы не просто заключаем сделки — мы выстраиваем долгосрочные отношения с клиентами и сообществами.",
                    null, null),
            }),

            PageContent("about.values", new (Language, string?, string?, string?, string?)[]
            {
                (Language.En, "Our Values",
                    "Integrity is the foundation of everything we do. We believe that buying or selling a home is one of life's most significant decisions, and every client deserves complete transparency throughout the process.\n\nWe are committed to excellence — continuously improving our market knowledge, negotiation skills and customer experience. Our team invests in ongoing education so we can always offer the most up-to-date advice.\n\nCommunity matters to us. Estoria actively supports local initiatives in Tallinn and believes that a thriving city is built on strong neighbourhoods.",
                    null, null),
                (Language.Et, "Meie väärtused",
                    "Ausus on kõige alus, mida teeme. Usume, et kodu ostmine või müümine on üks elu olulisemaid otsuseid ning iga klient väärib kogu protsessi vältel täielikku läbipaistvust.\n\nOleme pühendunud tipptasemele — pidevalt täiustades oma turuteadmisi, läbirääkimisoskusi ja kliendikogemust. Meie meeskond investeerib pidevasse haridusse, et alati pakkuda kõige ajakohasemat nõu.\n\nKogukond on meile tähtis. Estoria toetab aktiivselt kohalikke algatusi Tallinnas ja usub, et elav linn on üles ehitatud tugevatele linnaosadele.",
                    null, null),
                (Language.Ru, "Наши ценности",
                    "Честность — основа всего, что мы делаем. Мы убеждены, что покупка или продажа дома — одно из самых значимых решений в жизни, и каждый клиент заслуживает полной прозрачности на протяжении всего процесса.\n\nМы стремимся к превосходству — постоянно совершенствуя знания рынка, навыки переговоров и качество обслуживания. Наша команда вкладывает средства в непрерывное образование, чтобы всегда предлагать самые актуальные рекомендации.\n\nСообщество важно для нас. Estoria активно поддерживает местные инициативы в Таллине и считает, что процветающий город строится на сильных кварталах.",
                    null, null),
            }),

            PageContent("contact.info", new (Language, string?, string?, string?, string?)[]
            {
                (Language.En, "Contact Us",
                    "Estoria Real Estate\nKotzebue 4, Tallinn 10412, Estonia\nPhone: +372 600 1234\nEmail: info@estoria.estate\nOpen Mon–Fri 09:00–18:00, Sat 10:00–15:00",
                    null, null),
                (Language.Et, "Võtke meiega ühendust",
                    "Estoria Kinnisvara\nKotzebue 4, Tallinn 10412, Eesti\nTelefon: +372 600 1234\nE-post: info@estoria.estate\nAvatud E–R 09:00–18:00, L 10:00–15:00",
                    null, null),
                (Language.Ru, "Свяжитесь с нами",
                    "Estoria Недвижимость\nKotzebue 4, Таллин 10412, Эстония\nТелефон: +372 600 1234\nEmail: info@estoria.estate\nРабочие часы: пн–пт 09:00–18:00, сб 10:00–15:00",
                    null, null),
            }),
        };

        _db.PageContents.AddRange(pages);

        // ── 2. Team Members ───────────────────────────────────────────────────

        var agent1 = new TeamMember
        {
            Slug      = "martin-tamm",
            Phone     = "+372 5123 4567",
            Email     = "martin.tamm@estoria.estate",
            Languages = ["et", "en", "ru"],
            SortOrder = 1,
            IsActive  = true,
            Translations =
            [
                new TeamMemberTranslation
                {
                    Language = Language.En,
                    Name     = "Martin Tamm",
                    Role     = "Senior Real Estate Agent",
                    Bio      = "Martin has over 12 years of experience in Tallinn's residential market. Specialising in Kesklinn and Kadriorg, he has a deep understanding of price trends and buyer expectations. Martin is known for his patient, no-pressure approach and has been recognised as a top agent for four consecutive years."
                },
                new TeamMemberTranslation
                {
                    Language = Language.Et,
                    Name     = "Martin Tamm",
                    Role     = "Vanemmaakler",
                    Bio      = "Martinil on üle 12-aastane kogemus Tallinna elamuturul. Spetsialiseerunud Kesklinnale ja Kadriorile, omab ta sügavat arusaama hinnatrendidest ja ostjate ootustest. Martin on tuntud oma kannatlikust ja survevabast lähenemisest ning on neljal järjestikusel aastal tunnustatud tippmaaklerina."
                },
                new TeamMemberTranslation
                {
                    Language = Language.Ru,
                    Name     = "Мартин Тамм",
                    Role     = "Старший агент по недвижимости",
                    Bio      = "У Мартина более 12 лет опыта работы на жилом рынке Таллина. Специализируясь на районах Кесклинн и Кадриорг, он глубоко понимает тенденции цен и ожидания покупателей. Мартин известен своим терпеливым подходом без давления и четыре года подряд признаётся лучшим агентом."
                },
            ]
        };

        var agent2 = new TeamMember
        {
            Slug      = "liis-kask",
            Phone     = "+372 5234 5678",
            Email     = "liis.kask@estoria.estate",
            Languages = ["et", "en"],
            SortOrder = 2,
            IsActive  = true,
            Translations =
            [
                new TeamMemberTranslation
                {
                    Language = Language.En,
                    Name     = "Liis Kask",
                    Role     = "Real Estate Agent",
                    Bio      = "Liis joined Estoria in 2018 after a career in interior design. Her eye for quality and space planning gives clients a unique perspective when viewing properties. She focuses on Kalamaja, Telliskivi and Põhja-Tallinn — areas she knows and loves intimately."
                },
                new TeamMemberTranslation
                {
                    Language = Language.Et,
                    Name     = "Liis Kask",
                    Role     = "Kinnisvaramaakler",
                    Bio      = "Liis liitus Estoriaga 2018. aastal pärast karjääri interjööridisainis. Tema silm kvaliteedi ja ruumiplaneerimise jaoks annab klientidele ainulaadse perspektiivi kinnisvara vaatamisel. Ta keskendub Kalamajale, Telliskivile ja Põhja-Tallinnale — piirkondadele, mida ta tunneb ja armastab."
                },
                new TeamMemberTranslation
                {
                    Language = Language.Ru,
                    Name     = "Лийс Каск",
                    Role     = "Агент по недвижимости",
                    Bio      = "Лийс присоединилась к Estoria в 2018 году после карьеры в области дизайна интерьеров. Её чувство качества и планировки пространства даёт клиентам уникальный взгляд при просмотре объектов. Она специализируется на районах Каламая, Теллискиви и Пыхья-Таллинн."
                },
            ]
        };

        var agent3 = new TeamMember
        {
            Slug      = "andrei-volkov",
            Phone     = "+372 5345 6789",
            Email     = "andrei.volkov@estoria.estate",
            Languages = ["et", "en", "ru"],
            SortOrder = 3,
            IsActive  = true,
            Translations =
            [
                new TeamMemberTranslation
                {
                    Language = Language.En,
                    Name     = "Andrei Volkov",
                    Role     = "Real Estate Agent & Valuations Specialist",
                    Bio      = "Andrei brings 8 years of experience in property valuation and investment analysis. Fluent in Estonian, English and Russian, he is particularly valued by international clients navigating the Estonian market. His background in finance ensures clients receive thorough due-diligence support on every transaction."
                },
                new TeamMemberTranslation
                {
                    Language = Language.Et,
                    Name     = "Andrei Volkov",
                    Role     = "Kinnisvaramaakler ja hindamisspetsialist",
                    Bio      = "Andrei toob kaasa 8-aastase kogemuse kinnisvara hindamise ja investeeringute analüüsi valdkonnas. Ladusalt eesti, inglise ja vene keelt kõneldes on ta eriti hinnatud rahvusvaheliste klientide seas, kes navigeerivad Eesti turul. Tema finantstaustaga kliendid saavad iga tehingu puhul põhjaliku hoolsuskohustuse toe."
                },
                new TeamMemberTranslation
                {
                    Language = Language.Ru,
                    Name     = "Андрей Волков",
                    Role     = "Агент по недвижимости и специалист по оценке",
                    Bio      = "Андрей привносит 8-летний опыт в области оценки недвижимости и инвестиционного анализа. Свободно владея эстонским, английским и русским языками, он особенно ценится международными клиентами, работающими на эстонском рынке. Его финансовый опыт обеспечивает клиентам комплексную поддержку при проверке каждой сделки."
                },
            ]
        };

        _db.TeamMembers.AddRange(agent1, agent2, agent3);

        // ── 3. Services ───────────────────────────────────────────────────────

        var services = new List<Service>
        {
            new Service
            {
                Slug      = "property-sales",
                IconName  = "home",
                SortOrder = 1,
                IsActive  = true,
                Translations =
                [
                    new ServiceTranslation { Language = Language.En, Name = "Property Sales",
                        Description = "We guide you through every step of selling your property — from accurate pricing and professional photography to negotiation and notarial deed. Our market analysis ensures you achieve the best possible price in the shortest time.",
                        PriceInfo   = "Commission from 2% + VAT" },
                    new ServiceTranslation { Language = Language.Et, Name = "Kinnisvara müük",
                        Description = "Juhendame teid läbi iga müügisammu — täpsest hinnakujundusest ja professionaalsest fotograafiast kuni läbirääkimiste ja notariaaktini. Meie turuanalüüs tagab parima võimaliku hinna lühima ajaga.",
                        PriceInfo   = "Vahendustasu alates 2% + käibemaks" },
                    new ServiceTranslation { Language = Language.Ru, Name = "Продажа недвижимости",
                        Description = "Мы сопровождаем вас на каждом этапе продажи — от точного ценообразования и профессиональной фотосъёмки до переговоров и нотариального акта. Наш анализ рынка гарантирует максимально выгодную цену в кратчайшие сроки.",
                        PriceInfo   = "Комиссия от 2% + НДС" },
                ]
            },
            new Service
            {
                Slug      = "property-rental",
                IconName  = "key",
                SortOrder = 2,
                IsActive  = true,
                Translations =
                [
                    new ServiceTranslation { Language = Language.En, Name = "Property Rental",
                        Description = "Whether you are a landlord seeking reliable tenants or a renter looking for the ideal home, our rental team handles the entire process — listings, viewings, background checks, lease agreements and ongoing management.",
                        PriceInfo   = "One month's rent (landlord) | Free for tenants" },
                    new ServiceTranslation { Language = Language.Et, Name = "Kinnisvara üürimine",
                        Description = "Olgu te üürileandja, kes otsib usaldusväärseid üürnikke, või üürnik, kes otsib ideaalset kodu — meie üüritiim haldab kogu protsessi: kuulutused, vaatamised, taustakontrollid, üürilepingud ja jooksvad haldusküsimused.",
                        PriceInfo   = "Ühe kuu üür (üürileandja) | Üürnikule tasuta" },
                    new ServiceTranslation { Language = Language.Ru, Name = "Аренда недвижимости",
                        Description = "Будь то арендодатель, ищущий надёжных жильцов, или арендатор в поисках идеального жилья — наша команда по аренде берёт на себя весь процесс: объявления, просмотры, проверку биографии, договоры аренды и текущее управление.",
                        PriceInfo   = "Один месяц аренды (арендодатель) | Бесплатно для арендаторов" },
                ]
            },
            new Service
            {
                Slug      = "property-valuation",
                IconName  = "calculator",
                SortOrder = 3,
                IsActive  = true,
                Translations =
                [
                    new ServiceTranslation { Language = Language.En, Name = "Property Valuation",
                        Description = "Our certified valuators provide independent, bank-recognised property valuations for purchase, sale, refinancing, inheritance and insurance purposes. Reports are delivered within 3 business days and comply with Estonian and EU standards.",
                        PriceInfo   = "From €180 incl. VAT" },
                    new ServiceTranslation { Language = Language.Et, Name = "Kinnisvara hindamine",
                        Description = "Meie sertifitseeritud hindajad pakuvad sõltumatuid, pankade poolt tunnustatud kinnisvarahindamisi ostu, müügi, refinantseerimise, pärimise ja kindlustuse eesmärgil. Aruanded edastatakse 3 tööpäeva jooksul ja vastavad Eesti ning EL standarditele.",
                        PriceInfo   = "Alates 180 € koos käibemaksuga" },
                    new ServiceTranslation { Language = Language.Ru, Name = "Оценка недвижимости",
                        Description = "Наши сертифицированные оценщики проводят независимые, признанные банками оценки недвижимости для целей покупки, продажи, рефинансирования, наследования и страхования. Отчёты предоставляются в течение 3 рабочих дней в соответствии со стандартами Эстонии и ЕС.",
                        PriceInfo   = "От 180 € с НДС" },
                ]
            },
            new Service
            {
                Slug      = "legal-consulting",
                IconName  = "gavel",
                SortOrder = 4,
                IsActive  = true,
                Translations =
                [
                    new ServiceTranslation { Language = Language.En, Name = "Legal Consulting",
                        Description = "Navigating Estonian property law can be complex, especially for international buyers. Our partner law firm provides full legal support — title checks, purchase contract review, notarial representation and post-transaction advice — all under one roof.",
                        PriceInfo   = "From €120/hour incl. VAT" },
                    new ServiceTranslation { Language = Language.Et, Name = "Juriidiline nõustamine",
                        Description = "Eesti kinnisvaraseadustes orienteerumine võib olla keeruline, eriti rahvusvahelistele ostjatele. Meie partneradvokaatuuri pakub täielikku õiguslikku tuge — omandiõiguse kontroll, ostu-müügilepingu ülevaatus, notariaalne esindamine ja tehingujärgne nõustamine — kõik ühe katuse all.",
                        PriceInfo   = "Alates 120 €/tund koos käibemaksuga" },
                    new ServiceTranslation { Language = Language.Ru, Name = "Юридическое консультирование",
                        Description = "Разобраться в эстонском законодательстве о недвижимости может быть непросто, особенно для иностранных покупателей. Наша юридическая фирма-партнёр предоставляет полную правовую поддержку — проверку права собственности, проверку договора купли-продажи, нотариальное представительство и консультации после сделки — всё в одном месте.",
                        PriceInfo   = "От 120 €/час с НДС" },
                ]
            },
        };

        _db.Services.AddRange(services);

        // ── 4. Properties ─────────────────────────────────────────────────────

        var now = DateTime.UtcNow;

        var prop1 = new Property
        {
            Slug            = "penthouse-kesklinn-vabaduse-valljak",
            TransactionType = TransactionType.Sale,
            PropertyType    = PropertyType.Apartment,
            Status          = PropertyStatus.Active,
            Price           = 485_000m,
            Currency        = "EUR",
            Size            = 112m,
            Rooms           = 4,
            Bedrooms        = 3,
            Bathrooms       = 2,
            Floor           = 9,
            TotalFloors     = 9,
            YearBuilt       = 2019,
            EnergyClass     = "A",
            Latitude        = 59.4364,
            Longitude       = 24.7450,
            IsFeatured      = true,
            AgentId         = agent1.Id,
            PublishedAt     = now.AddDays(-10),
            Translations    =
            [
                new PropertyTranslation
                {
                    Language    = Language.En,
                    Title       = "Stunning Penthouse in the Heart of Tallinn",
                    Description = "This exceptional top-floor penthouse offers panoramic views over Tallinn's Old Town and the Baltic Sea. The open-plan living area flows seamlessly onto a private 40 m² terrace — perfect for entertaining or simply watching the city come alive.\n\nThe kitchen is finished with Miele appliances, stone worktops and bespoke cabinetry. All three bedrooms have built-in wardrobes; the master suite features an en-suite bathroom with a freestanding bathtub. Underfloor heating throughout.\n\nLocated in a secure, concierge-managed building with two underground parking spaces and a private storage unit. Walking distance to the Old Town, Central Market and major bus routes.",
                    Address     = "Vabaduse väljak 8",
                    City        = "Tallinn",
                    District    = "Kesklinn"
                },
                new PropertyTranslation
                {
                    Language    = Language.Et,
                    Title       = "Kaunis penthouse Tallinna südames",
                    Description = "See erakordne korrus-penthouse pakub panoraamvaadet Tallinna vanalinnale ja Läänemerele. Avaplaan eluruum avaneb sujuvalt 40 m² privaatterrassile — ideaalne seltskondlikuks koosviibimiseks või lihtsalt linna elu jälgimiseks.\n\nKöök on viimistletud Miele kodumasinatega, kiviplaatidest tööpindadega ja tellimuskabinetiga. Kõigil kolmel magamistupades on sisseehitatud riidekapid; põhimagamistoa juurde kuulub vannituba vabalt seisva vanniga. Põrandaküte läbi kõigi ruumide.\n\nAsub turvalises portjeega hoones kahe maa-aluse parkimiskohaga ja privaatse laoruum. Kõndimisel vanalinnast, keskturgust ja peamistest bussiühendustest.",
                    Address     = "Vabaduse väljak 8",
                    City        = "Tallinn",
                    District    = "Kesklinn"
                },
                new PropertyTranslation
                {
                    Language    = Language.Ru,
                    Title       = "Потрясающий пентхаус в самом сердце Таллина",
                    Description = "Этот исключительный пентхаус на верхнем этаже предлагает панорамный вид на Старый город Таллина и Балтийское море. Гостиная с открытой планировкой плавно переходит в частную террасу площадью 40 м² — идеальное место для развлечений или созерцания городской жизни.\n\nКухня оснащена техникой Miele, каменными столешницами и мебелью на заказ. Во всех трёх спальнях есть встроенные шкафы; в главной спальне — ванная комната с отдельно стоящей ванной. Тёплый пол во всех помещениях.\n\nРасположен в охраняемом доме с консьержем, двумя подземными парковочными местами и кладовой. В пешей доступности от Старого города, Центрального рынка и основных автобусных маршрутов.",
                    Address     = "Вабадузе вяльяк 8",
                    City        = "Таллин",
                    District    = "Кесклинн"
                },
            ],
            Images =
            [
                new PropertyImage { Url = "https://placehold.co/800x600?text=Penthouse+Living", SortOrder = 0, IsCover = true },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Penthouse+Terrace", SortOrder = 1 },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Penthouse+Kitchen", SortOrder = 2 },
            ],
            Features =
            [
                new PropertyFeature { Feature ="Underfloor heating" },
                new PropertyFeature { Feature ="Private terrace" },
                new PropertyFeature { Feature ="2 parking spaces" },
                new PropertyFeature { Feature ="Concierge service" },
                new PropertyFeature { Feature ="Storage unit" },
            ]
        };

        var prop2 = new Property
        {
            Slug            = "kalamaja-renoveeritud-korter",
            TransactionType = TransactionType.Sale,
            PropertyType    = PropertyType.Apartment,
            Status          = PropertyStatus.Active,
            Price           = 285_000m,
            Currency        = "EUR",
            Size            = 72m,
            Rooms           = 3,
            Bedrooms        = 2,
            Bathrooms       = 1,
            Floor           = 3,
            TotalFloors     = 5,
            YearBuilt       = 1937,
            EnergyClass     = "C",
            Latitude        = 59.4460,
            Longitude       = 24.7278,
            IsFeatured      = true,
            AgentId         = agent2.Id,
            PublishedAt     = now.AddDays(-7),
            Translations    =
            [
                new PropertyTranslation
                {
                    Language    = Language.En,
                    Title       = "Beautifully Renovated Apartment in Kalamaja",
                    Description = "Nestled in the heart of Tallinn's most sought-after creative district, this lovingly restored 1930s apartment blends period charm with modern comfort. Original hardwood floors and ornate ceiling mouldings have been carefully preserved, while the kitchen and bathroom are fully contemporary.\n\nThe generous open-plan kitchen-living room is flooded with afternoon light. Both bedrooms are quiet, looking onto the courtyard. The bathroom features Italian tiles, a walk-in shower and a heated towel rail.\n\nKalamaja offers independent boutiques, artisan coffee shops, galleries and weekend markets — all within a five-minute walk. Excellent tram connections to the city centre.",
                    Address     = "Kotzebue 18",
                    City        = "Tallinn",
                    District    = "Kalamaja"
                },
                new PropertyTranslation
                {
                    Language    = Language.Et,
                    Title       = "Ilusalt renoveeritud korter Kalamajas",
                    Description = "Tallinna nõutavaima loomekvartaliti südames asuv armastusega taastatud 1930ndate korter ühendab ajaloolise võlu kaasaegse mugavusega. Originaalsed puitmassiivsed põrandad ja kaunistatud laemuldingid on hoolikalt säilitatud, samal ajal kui köök ja vannituba on täiesti kaasaegsed.\n\nÜlirikas avaplaan köök-elutuba on ujutatud pärastlõunase valgusega. Mõlemad magamistoad on vaiksed, avanevad hooviküljele. Vannitoas on Itaalia plaadid, avatud dušš ja köetav rätikurestament.\n\nKalamaja pakub iseseisvaid butiike, käsitöö kohvikuid, galeriisid ja nädalavahetuse turge — kõik viie minuti jalutuskäigu kaugusel. Suurepärased trammiühendused kesklinnaga.",
                    Address     = "Kotzebue 18",
                    City        = "Tallinn",
                    District    = "Kalamaja"
                },
                new PropertyTranslation
                {
                    Language    = Language.Ru,
                    Title       = "Прекрасно отремонтированная квартира в Каламая",
                    Description = "Расположенная в самом сердце самого востребованного творческого района Таллина, эта бережно отреставрированная квартира 1930-х годов сочетает в себе исторический шарм и современный комфорт. Оригинальные паркетные полы и лепные потолки тщательно сохранены, а кухня и ванная комната полностью современные.\n\nПросторная кухня-гостиная с открытой планировкой наполнена послеобеденным светом. Обе спальни тихие, выходят во двор. В ванной — итальянская плитка, душ с открытым входом и полотенцесушитель.\n\nКаламая предлагает независимые бутики, кофейни, галереи и воскресные рынки — всё в пяти минутах ходьбы. Отличное трамвайное сообщение с центром города.",
                    Address     = "Котцебуэ 18",
                    City        = "Таллин",
                    District    = "Каламая"
                },
            ],
            Images =
            [
                new PropertyImage { Url = "https://placehold.co/800x600?text=Kalamaja+Living", SortOrder = 0, IsCover = true },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Kalamaja+Kitchen", SortOrder = 1 },
            ],
            Features =
            [
                new PropertyFeature { Feature ="Original hardwood floors" },
                new PropertyFeature { Feature ="Walk-in shower" },
                new PropertyFeature { Feature ="Courtyard view" },
            ]
        };

        var prop3 = new Property
        {
            Slug            = "pirita-perekodu",
            TransactionType = TransactionType.Sale,
            PropertyType    = PropertyType.House,
            Status          = PropertyStatus.Active,
            Price           = 498_000m,
            Currency        = "EUR",
            Size            = 195m,
            Rooms           = 6,
            Bedrooms        = 4,
            Bathrooms       = 3,
            Floor           = null,
            TotalFloors     = 2,
            YearBuilt       = 2015,
            EnergyClass     = "B",
            Latitude        = 59.4712,
            Longitude       = 24.8337,
            IsFeatured      = true,
            AgentId         = agent1.Id,
            PublishedAt     = now.AddDays(-14),
            Translations    =
            [
                new PropertyTranslation
                {
                    Language    = Language.En,
                    Title       = "Modern Family Home Near Pirita Beach",
                    Description = "A rare opportunity to own a contemporary detached house just 400 metres from Pirita's sandy beach and the Pirita River promenade. Built in 2015 to a high specification, this four-bedroom home is in turn-key condition and ready for immediate occupation.\n\nThe ground floor features a large open-plan kitchen-dining-living area with direct access to the landscaped garden and terrace. A dedicated home office, utility room and guest WC complete the ground floor. Upstairs, the spacious master bedroom enjoys views over the tree-lined street, with an en-suite bathroom and walk-in wardrobe.\n\nThe property includes a double garage, a 500 m² plot with mature fruit trees, and solar panels that cover most of the household energy needs. Top-rated school catchment area; 20 minutes by car to Tallinn city centre.",
                    Address     = "Mähe tee 7",
                    City        = "Tallinn",
                    District    = "Pirita"
                },
                new PropertyTranslation
                {
                    Language    = Language.Et,
                    Title       = "Kaasaegne perekodu Pirita ranna lähedal",
                    Description = "Harukordne võimalus omandada kaasaegne eramu vaid 400 meetri kaugusel Pirita liivamäerandast ja Pirita jõe promenaadist. 2015. aastal kõrge spetsifikatsiooniga ehitatud neljamagamistubane kodu on võtmevalmis ja koheseks kasutusse võtmiseks valmis.\n\nMaapinnal on suur avaplaan köök-söögituba-elutuba koos otsepääsuga maastikukujundatud aeda ja terrassile. Eraldi kodukontori, pesemistuba ja külalistualet täiendavad maapinda. Üleval tagab avar põhimagamistuba vaateid puudega vooderdatud tänavale, koos vannitoaga ja jalutuskapiga.\n\nKinnisvara sisaldab topelt garaaži, 500 m² maatükki küpsete viljapuudega ja päikesepaneele, mis katavad enamiku majapidamise energiavajadustest. Kõrge reitinguga koolide haardealal; 20 minutit autoga Tallinna kesklinnani.",
                    Address     = "Mähe tee 7",
                    City        = "Tallinn",
                    District    = "Pirita"
                },
                new PropertyTranslation
                {
                    Language    = Language.Ru,
                    Title       = "Современный семейный дом недалеко от пляжа Пирита",
                    Description = "Редкая возможность приобрести современный отдельный дом всего в 400 метрах от песчаного пляжа Пирита и набережной реки Пирита. Построенный в 2015 году по высоким стандартам, этот четырёхспальный дом в безупречном состоянии готов к немедленному заселению.\n\nНа первом этаже расположена большая кухня-столовая-гостиная с открытой планировкой и прямым выходом в ухоженный сад и на террасу. Отдельный домашний офис, подсобное помещение и гостевой туалет завершают первый этаж. На втором этаже просторная главная спальня с видом на тенистую улицу, ванной комнатой и гардеробной.\n\nОбъект включает двойной гараж, участок 500 м² со зрелыми плодовыми деревьями и солнечные панели, покрывающие большую часть потребностей домохозяйства в энергии. Зона обслуживания ведущих школ; 20 минут на машине до центра Таллина.",
                    Address     = "Мяхе тее 7",
                    City        = "Таллин",
                    District    = "Пирита"
                },
            ],
            Images =
            [
                new PropertyImage { Url = "https://placehold.co/800x600?text=Pirita+Exterior", SortOrder = 0, IsCover = true },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Pirita+Garden", SortOrder = 1 },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Pirita+Living", SortOrder = 2 },
            ],
            Features =
            [
                new PropertyFeature { Feature ="Double garage" },
                new PropertyFeature { Feature ="Solar panels" },
                new PropertyFeature { Feature ="500 m² plot" },
                new PropertyFeature { Feature ="Landscaped garden" },
                new PropertyFeature { Feature ="Home office" },
            ]
        };

        var prop4 = new Property
        {
            Slug            = "kadriorg-klassikaline-korter",
            TransactionType = TransactionType.Sale,
            PropertyType    = PropertyType.Apartment,
            Status          = PropertyStatus.Active,
            Price           = 340_000m,
            Currency        = "EUR",
            Size            = 88m,
            Rooms           = 3,
            Bedrooms        = 2,
            Bathrooms       = 2,
            Floor           = 4,
            TotalFloors     = 6,
            YearBuilt       = 2007,
            EnergyClass     = "B",
            Latitude        = 59.4416,
            Longitude       = 24.7787,
            IsFeatured      = false,
            AgentId         = agent3.Id,
            PublishedAt     = now.AddDays(-5),
            Translations    =
            [
                new PropertyTranslation
                {
                    Language    = Language.En,
                    Title       = "Elegant Apartment with Park Views in Kadriorg",
                    Description = "Set in one of Tallinn's most prestigious addresses, this bright apartment looks directly onto the century-old lime trees of Kadriorg Park. The layout is efficient yet generous — two double bedrooms, two bathrooms and a large corner living room with floor-to-ceiling windows.\n\nThe kitchen has been recently updated with integrated appliances and quartz countertops. Both bathrooms are finished to a high standard with heated floors. A private balcony off the master bedroom is the perfect spot for morning coffee.\n\nThe building has a lift, a bike storage room and secure coded entry. Just a few minutes' walk to the KUMU Art Museum, Kadriorg Palace and the coastal promenade.",
                    Address     = "Weizenbergi 34",
                    City        = "Tallinn",
                    District    = "Kadriorg"
                },
                new PropertyTranslation
                {
                    Language    = Language.Et,
                    Title       = "Elegantne korter pargiga vaatega Kadriorgs",
                    Description = "Ühes Tallinna prestiižsemas aadressis asuv ere korter vaatab otse Kadrioru pargi sajandi vanuste pärnapuude peale. Paigutus on efektiivne, kuid avar — kaks kahevoodilist magamistuba, kaks vannituba ja suur nurgas asuv elutuba põrandast laeni ulatuvate akendega.\n\nKöök on hiljuti uuendatud integreeritud kodumashinate ja kvartstööpindadega. Mõlemad vannitoad on kõrge standardi järgi viimistletud köetud põrandatega. Privaatne rõdu põhimagamistoa juurde on ideaalne koht hommikukohvi jaoks.\n\nHoonel on lift, rattahoidla ja turvaline koodiga sissepääs. Vaid paar minutit jalutades KUMU Kunstimuuseumini, Kadrioru paleeteni ja rannapromenaadini.",
                    Address     = "Weizenbergi 34",
                    City        = "Tallinn",
                    District    = "Kadriorg"
                },
                new PropertyTranslation
                {
                    Language    = Language.Ru,
                    Title       = "Элегантная квартира с видом на парк в Кадриорге",
                    Description = "Расположенная по одному из самых престижных адресов Таллина, эта светлая квартира выходит прямо на вековые липы Кадриоргского парка. Планировка эффективная, но просторная — две двуспальные спальни, две ванных комнаты и большая угловая гостиная с окнами от пола до потолка.\n\nКухня недавно обновлена встроенной техникой и столешницами из кварца. Обе ванные комнаты отделаны по высокому стандарту с тёплыми полами. Частный балкон при главной спальне — идеальное место для утреннего кофе.\n\nВ здании есть лифт, место для хранения велосипедов и безопасный вход с кодом. Всего несколько минут пешком до Художественного музея KUMU, Кадриоргского дворца и прибрежной набережной.",
                    Address     = "Вейзенберги 34",
                    City        = "Таллин",
                    District    = "Кадриорг"
                },
            ],
            Images =
            [
                new PropertyImage { Url = "https://placehold.co/800x600?text=Kadriorg+Living", SortOrder = 0, IsCover = true },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Kadriorg+Balcony", SortOrder = 1 },
            ],
            Features =
            [
                new PropertyFeature { Feature ="Park view" },
                new PropertyFeature { Feature ="Lift" },
                new PropertyFeature { Feature ="Heated bathroom floors" },
                new PropertyFeature { Feature ="Private balcony" },
            ]
        };

        var prop5 = new Property
        {
            Slug            = "nomme-vaike-maja-aiaga",
            TransactionType = TransactionType.Sale,
            PropertyType    = PropertyType.House,
            Status          = PropertyStatus.Active,
            Price           = 310_000m,
            Currency        = "EUR",
            Size            = 148m,
            Rooms           = 5,
            Bedrooms        = 3,
            Bathrooms       = 2,
            Floor           = null,
            TotalFloors     = 2,
            YearBuilt       = 1965,
            EnergyClass     = "D",
            Latitude        = 59.3944,
            Longitude       = 24.6875,
            IsFeatured      = false,
            AgentId         = agent2.Id,
            PublishedAt     = now.AddDays(-20),
            Translations    =
            [
                new PropertyTranslation
                {
                    Language    = Language.En,
                    Title       = "Charming Detached House with Garden in Nõmme",
                    Description = "Nõmme is Tallinn's green lung — a quiet, forested suburb with its own distinct village character. This well-maintained 1960s house sits on a 900 m² plot surrounded by mature pines, offering genuine seclusion while remaining within the city limits.\n\nThe interior retains its original character: solid timber construction, a traditional tile stove in the living room and a bright sunroom overlooking the garden. The upper floor has been thoughtfully modernised with new windows and insulation.\n\nPerfect for families or anyone seeking space, nature and tranquillity. Nõmme train station is a seven-minute walk, providing a direct 25-minute connection to Tallinn's Baltic Station.",
                    Address     = "Männiku tee 43",
                    City        = "Tallinn",
                    District    = "Nõmme"
                },
                new PropertyTranslation
                {
                    Language    = Language.Et,
                    Title       = "Armas eramu aiaga Nõmmel",
                    Description = "Nõmme on Tallinna roheline kops — vaikne, metsane eeslinn oma eristava külakarakteriga. See hästi hooldatud 1960ndate maja asub 900 m² krundil, mida ümbritsevad täiskasvanud männid, pakkudes tõelist eraldatust, jäädes samal ajal linna piiresse.\n\nSisemus säilitab oma algse karakteri: tahke puitkonstruktsioon, traditsiooniline kahhelahi elutoas ja ere suurepärane talveaed aiaga vaatega. Ülemine korrus on läbimõeldult moderniseeritud uute akende ja soojustusega.\n\nIdeaalne peredele või kõigile, kes otsivad ruumi, loodust ja rahulikkust. Nõmme raudteejaam on seitsme minuti jalutuskäigu kaugusel, pakkudes otsühendust Tallinna Balti jaama 25 minutiga.",
                    Address     = "Männiku tee 43",
                    City        = "Tallinn",
                    District    = "Nõmme"
                },
                new PropertyTranslation
                {
                    Language    = Language.Ru,
                    Title       = "Уютный отдельный дом с садом в Нымме",
                    Description = "Нымме — зелёные лёгкие Таллина, тихий лесной пригород со своим особым деревенским характером. Этот хорошо ухоженный дом 1960-х годов расположен на участке 900 м², окружённом зрелыми соснами, предлагая настоящее уединение в пределах городской черты.\n\nИнтерьер сохраняет свой первоначальный характер: прочная деревянная конструкция, традиционная изразцовая печь в гостиной и светлая веранда с видом на сад. Верхний этаж продуманно модернизирован с новыми окнами и утеплением.\n\nИдеально для семей или всех, кто ищет пространство, природу и спокойствие. Станция Нымме в семи минутах ходьбы, с прямым сообщением до Балтийского вокзала Таллина за 25 минут.",
                    Address     = "Мянникю тее 43",
                    City        = "Таллин",
                    District    = "Нымме"
                },
            ],
            Images =
            [
                new PropertyImage { Url = "https://placehold.co/800x600?text=Nomme+House", SortOrder = 0, IsCover = true },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Nomme+Garden", SortOrder = 1 },
            ],
            Features =
            [
                new PropertyFeature { Feature ="900 m² plot" },
                new PropertyFeature { Feature ="Mature pine garden" },
                new PropertyFeature { Feature ="Traditional tile stove" },
                new PropertyFeature { Feature ="Sunroom" },
            ]
        };

        var prop6 = new Property
        {
            Slug            = "kristiine-uus-korter-uurimiseks",
            TransactionType = TransactionType.Rent,
            PropertyType    = PropertyType.Apartment,
            Status          = PropertyStatus.Active,
            Price           = 1_250m,
            Currency        = "EUR",
            Size            = 58m,
            Rooms           = 2,
            Bedrooms        = 1,
            Bathrooms       = 1,
            Floor           = 2,
            TotalFloors     = 4,
            YearBuilt       = 2021,
            EnergyClass     = "A",
            Latitude        = 59.4179,
            Longitude       = 24.7134,
            IsFeatured      = true,
            AgentId         = agent3.Id,
            PublishedAt     = now.AddDays(-3),
            Translations    =
            [
                new PropertyTranslation
                {
                    Language    = Language.En,
                    Title       = "Modern 2-Room Apartment for Rent in Kristiine",
                    Description = "Newly built in 2021, this bright apartment in the up-and-coming Kristiine district is available unfurnished or furnished on request. The open-plan kitchen-living room features high-end appliances, engineered oak flooring and large double-glazed windows.\n\nThe bedroom comfortably accommodates a king-size bed with room for a desk. The bathroom has a rainfall shower, underfloor heating and ample storage. A private parking space in the underground garage is included in the rent.\n\nKristiine is emerging as one of Tallinn's most liveable districts, with new cycling paths, parks and a weekly farmers' market. Tram stop directly outside the building; 10 minutes to the city centre.",
                    Address     = "Järvevana tee 9",
                    City        = "Tallinn",
                    District    = "Kristiine"
                },
                new PropertyTranslation
                {
                    Language    = Language.Et,
                    Title       = "Kaasaegne 2-toaline korter üürile Kristiines",
                    Description = "2021. aastal valminud ere korter areneval Kristiine linnaosas on saadaval sisustamata või soovi korral sisustatud kujul. Avaplaan köök-elutoas on kõrgklassi kodumasinad, tehispuust tamme põrandad ja suured topeltklaasiga aknad.\n\nMagamistuba mahutab mugavalt kuningliku voodi koos ruumiga kirjutuslaua jaoks. Vannitoas on vihmadušš, põrandaküte ja piisavalt säilitusruumi. Üüri sisse on arvestatud privaatne parkimiskoht maa-aluses garaažis.\n\nKristiine kerkib esile ühe Tallinna enim elamisväärsema linnaosana uute kergliiklusteede, parkide ja iganädalase taluturguga. Trammipiatus otse maja ees; 10 minutit kesklinna.",
                    Address     = "Järvevana tee 9",
                    City        = "Tallinn",
                    District    = "Kristiine"
                },
                new PropertyTranslation
                {
                    Language    = Language.Ru,
                    Title       = "Современная 2-комнатная квартира в аренду в Кристийне",
                    Description = "Построенная в 2021 году, эта светлая квартира в развивающемся районе Кристийне сдаётся без мебели или с мебелью по запросу. В кухне-гостиной с открытой планировкой — высококачественная техника, инженерный дубовый паркет и большие окна с двойным остеклением.\n\nВ спальне комфортно умещается кровать king-size с местом для рабочего стола. В ванной комнате — душ с тропическим эффектом, тёплый пол и достаточно места для хранения. Частное парковочное место в подземном гараже включено в арендную плату.\n\nКристийне становится одним из самых комфортных районов Таллина с новыми велодорожками, парками и еженедельным фермерским рынком. Остановка трамвая прямо у дома; 10 минут до центра города.",
                    Address     = "Ярвевана тее 9",
                    City        = "Таллин",
                    District    = "Кристийне"
                },
            ],
            Images =
            [
                new PropertyImage { Url = "https://placehold.co/800x600?text=Kristiine+Living", SortOrder = 0, IsCover = true },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Kristiine+Bedroom", SortOrder = 1 },
                new PropertyImage { Url = "https://placehold.co/800x600?text=Kristiine+Kitchen", SortOrder = 2 },
            ],
            Features =
            [
                new PropertyFeature { Feature ="Underground parking" },
                new PropertyFeature { Feature ="Rainfall shower" },
                new PropertyFeature { Feature ="Engineered oak flooring" },
                new PropertyFeature { Feature ="Available furnished" },
            ]
        };

        _db.Properties.AddRange(prop1, prop2, prop3, prop4, prop5, prop6);

        // ── 5. Blog Posts ─────────────────────────────────────────────────────

        var blog1 = new BlogPost
        {
            Slug          = "tallinn-real-estate-market-2025",
            CoverImageUrl = "https://placehold.co/1200x630?text=Market+2025",
            AuthorId      = agent1.Id,
            Status        = BlogPostStatus.Published,
            PublishedAt   = now.AddDays(-30),
            Translations  =
            [
                new BlogPostTranslation
                {
                    Language        = Language.En,
                    Title           = "Tallinn Real Estate Market 2025: Trends and Opportunities",
                    Excerpt         = "After two years of correction, Tallinn's property market is showing signs of renewed confidence. Here is what buyers and investors need to know.",
                    Content         = "<p>The Tallinn residential market entered 2025 in a more balanced state than at any point since the post-pandemic boom. Transaction volumes in the fourth quarter of 2024 recovered to within 8% of their five-year average — a meaningful improvement after two successive years of contraction.</p><p><strong>Price stability in core districts</strong><br/>Kesklinn, Kadriorg and Kalamaja have demonstrated the strongest price resilience. Prime apartments in these areas held their values or recorded modest gains of 2–4%, supported by persistent undersupply of high-quality stock relative to qualified demand.</p><p><strong>Emerging opportunities</strong><br/>Kristiine and Põhja-Tallinn are attracting developer attention for the first time in several years. New mixed-use projects and improved public transport links are driving both end-user and investor demand. Yields in these districts now average 4.8–5.4% — among the most attractive in the city.</p><p><strong>Interest rates and affordability</strong><br/>The Euribor decline that began in late 2024 has meaningfully improved affordability for financed buyers. Our analysis suggests that a further 25–50 bps reduction would bring a significant tranche of previously sidelined buyers back into the market.</p><p>Overall, we view 2025 as a year of selective opportunity. Well-located, well-maintained properties at fair prices are moving quickly; overpriced or poorly presented listings continue to sit. Buyers who act decisively in the first half of the year are likely to look back with satisfaction.</p>",
                    MetaTitle       = "Tallinn Real Estate Market 2025 | Estoria",
                    MetaDescription = "Analysis of Tallinn's 2025 property market trends, price movements and investment opportunities from Estoria's senior agents."
                },
                new BlogPostTranslation
                {
                    Language        = Language.Et,
                    Title           = "Tallinna kinnisvaraturg 2025: trendid ja võimalused",
                    Excerpt         = "Pärast kahte aastat korrektsiooni näitab Tallinna kinnisvaraturg uuenenud usalduse märke. Siin on see, mida ostjad ja investorid peavad teadma.",
                    Content         = "<p>Tallinna elamuarendturg sisenes 2025. aastasse tasakaalustatumas seisus kui ükski punkt pärast pandeemiajärgset buumi. Tehingute mahud 2024. aasta neljandas kvartalis taastusid viie aasta keskmisest 8% piires — märkimisväärne paranemine pärast kahte järjestikust kokkutõmbumisaastat.</p><p><strong>Hinnastabiilsus põhipiirkondades</strong><br/>Kesklinn, Kadriorg ja Kalamaja on näidanud tugevaima hinnavastupidavuse. Nende piirkondade esmaklassilised korterid hoidsid oma väärtusi või registreerisid tagasihoidlikke kasvu 2–4%, toetatud kvaliteetse pakkumise püsivast puudujäägist võrreldes kvalifitseeritud nõudlusega.</p><p><strong>Uued võimalused</strong><br/>Kristiine ja Põhja-Tallinn köidavad arendajate tähelepanu esimest korda mitme aasta jooksul. Uued segakasutusprojektid ja parandatud ühistranspordiühendused soodustavad nii lõppkasutajate kui ka investorite nõudlust. Tootlused nendes piirkondades on nüüd keskmiselt 4,8–5,4% — ühed atraktiivsemad linnas.</p><p>Kokkuvõttes näeme 2025. aasta selektiivse võimaluste aastana. Hästi asuvad, hästi hooldatud kinnisvara õiglaste hindadega liiguvad kiiresti; üle hinnastatud või halvasti esitletud kuulutused jätkuvad istumist. Ostjad, kes tegutsevad otsustavalt aasta esimesel poolel, vaatavad tõenäoliselt tagasi rahuloluga.</p>",
                    MetaTitle       = "Tallinna kinnisvaraturg 2025 | Estoria",
                    MetaDescription = "Analüüs Tallinna 2025. aasta kinnisvaraturu trendidest, hindade liikumisest ja investeerimisvõimalustest Estoria vanemmaakleritelt."
                },
                new BlogPostTranslation
                {
                    Language        = Language.Ru,
                    Title           = "Рынок недвижимости Таллина 2025: тенденции и возможности",
                    Excerpt         = "После двух лет коррекции рынок недвижимости Таллина демонстрирует признаки восстановления уверенности. Вот что нужно знать покупателям и инвесторам.",
                    Content         = "<p>Жилой рынок Таллина вошёл в 2025 год в более сбалансированном состоянии, чем в любой момент после послепандемийного бума. Объёмы сделок в четвёртом квартале 2024 года восстановились до уровня в пределах 8% от среднего показателя за пять лет — значительное улучшение после двух последовательных лет сокращения.</p><p><strong>Стабильность цен в ключевых районах</strong><br/>Кесклинн, Кадриорг и Каламая продемонстрировали наибольшую ценовую устойчивость. Элитные квартиры в этих районах удержали свои позиции или зафиксировали скромный рост на 2–4%, поддерживаемый стойким дефицитом качественного предложения по отношению к квалифицированному спросу.</p><p><strong>Новые возможности</strong><br/>Кристийне и Пыхья-Таллинн впервые за несколько лет привлекают внимание застройщиков. Новые многофункциональные проекты и улучшенные маршруты общественного транспорта стимулируют спрос как конечных пользователей, так и инвесторов. Доходность в этих районах в среднем составляет 4,8–5,4% — одна из самых привлекательных в городе.</p><p>В целом мы рассматриваем 2025 год как год избирательных возможностей. Хорошо расположенная и хорошо ухоженная недвижимость по справедливым ценам движется быстро; переоценённые или плохо представленные объявления продолжают простаивать. Покупатели, которые действуют решительно в первой половине года, вероятно, оглянутся с удовлетворением.</p>",
                    MetaTitle       = "Рынок недвижимости Таллина 2025 | Estoria",
                    MetaDescription = "Анализ тенденций рынка недвижимости Таллина в 2025 году, движения цен и инвестиционных возможностей от старших агентов Estoria."
                },
            ]
        };

        var blog2 = new BlogPost
        {
            Slug          = "guide-buying-first-home-estonia",
            CoverImageUrl = "https://placehold.co/1200x630?text=First+Home+Guide",
            AuthorId      = agent2.Id,
            Status        = BlogPostStatus.Published,
            PublishedAt   = now.AddDays(-60),
            Translations  =
            [
                new BlogPostTranslation
                {
                    Language        = Language.En,
                    Title           = "A Complete Guide to Buying Your First Home in Estonia",
                    Excerpt         = "Buying property in Estonia is a transparent and secure process — but there are important steps, costs and pitfalls every first-time buyer should understand.",
                    Content         = "<p>Estonia has one of the most straightforward property transaction systems in Europe, underpinned by a fully digital land register and a robust notarial system. For a first-time buyer, understanding the process demystifies what can otherwise feel overwhelming.</p><h2>Step 1: Budget and financing</h2><p>Start by understanding your total budget — not just the purchase price, but all associated costs. In Estonia these typically include notarial fees (0.3–0.5% of the transaction value), state registration fees (0.4%), a transfer tax for non-residents, and legal fees if you use a solicitor. Bank financing is widely available; most lenders require a 15–20% deposit for primary residences.</p><h2>Step 2: Choosing the right area</h2><p>Each Tallinn district has its own character, price point and lifestyle profile. Kesklinn offers city convenience at premium prices; Kalamaja and Põhja-Tallinn are vibrant and artistic; Kadriorg and Pirita are greener and more family-oriented; Nõmme is the quietest and most village-like. Consider your daily commute, school catchments and the lifestyle you want.</p><h2>Step 3: The purchase agreement</h2><p>In Estonia, property transactions must be notarised. After agreeing a price, you will sign a preliminary agreement (eellepingut) and pay a deposit — typically 10%. The notarial deed is signed four to eight weeks later. The notary is an independent public official who verifies identities, explains the document in detail and ensures the transaction complies with the law.</p><h2>Step 4: Registration and handover</h2><p>After signing, the notary registers the new owner in the land register (kinnistusraamat) — often within the same day. Keys are handed over on the agreed date, and the transaction is complete.</p><p>Our team at Estoria is happy to accompany you through every step. Contact us for a free initial consultation.</p>",
                    MetaTitle       = "Guide to Buying Your First Home in Estonia | Estoria",
                    MetaDescription = "Everything first-time buyers need to know about purchasing property in Estonia — costs, process, notary and tips from Estoria's agents."
                },
                new BlogPostTranslation
                {
                    Language        = Language.Et,
                    Title           = "Täielik juhend oma esimese kodu ostmiseks Eestis",
                    Excerpt         = "Kinnisvara ostmine Eestis on läbipaistev ja turvaline protsess — kuid on olulisi samme, kulusid ja lõkse, mida iga esmakordne ostja peaks mõistma.",
                    Content         = "<p>Eestil on üks Euroopa läbipaistvamaid kinnisvaratehingute süsteeme, mis põhineb täielikult digitaalsel kinnistusraamatul ja tugevas notarisüsteemil. Esmakordse ostja jaoks muudab protsessi mõistmine demüstifitseeritud selle, mis muidu võib tunduda valdav.</p><h2>1. samm: Eelarve ja rahastamine</h2><p>Alustage oma kogumahust arusaamisest — mitte ainult ostuhinnast, vaid kõigist seotud kuludest. Eestis hõlmavad need tavaliselt notaritasusid (0,3–0,5% tehinguväärtusest), riigi registreerimistasusid (0,4%), mitteelanike tulumaksu ja juriidilisi tasusid, kui kasutate advokaati. Pangalaenamine on laialdaselt kättesaadav; enamik laenuandjaid nõuab põhielukohtade jaoks 15–20% sissemakset.</p><h2>2. samm: Õige piirkonna valimine</h2><p>Igal Tallinna linnaosaga on oma iseloom, hinnapunkt ja elustiiliprofiil. Kesklinn pakub linna mugavust lisatasu hindadega; Kalamaja ja Põhja-Tallinn on elav ja kunstiline; Kadriorg ja Pirita on rohelisemad ja pereorienteeritumad; Nõmme on vaikseima ja külalikuma iseloomuga. Kaaluge oma igapäevast töökommentaari, koolide haardealasid ja elustiili, mida soovite.</p><h2>3. samm: Ostuleping</h2><p>Eestis tuleb kinnisvaratehingud notariaalselt tõestada. Pärast hinna kokkuleppimist allkirjastate eellepingut ja maksate tagatisraha — tavaliselt 10%. Notariaalakt allkirjastatakse neli kuni kaheksa nädalat hiljem. Notar on sõltumatu avalik ametnik, kes kontrollib identiteete, selgitab dokumenti üksikasjalikult ja tagab, et tehing vastab seadusele.</p><h2>4. samm: Registreerimine ja üleandmine</h2><p>Pärast allkirjastamist registreerib notar uue omaniku kinnistusraamatusse — sageli sama päeva jooksul. Võtmed antakse üle kokkulepitud kuupäeval ja tehing on lõpetatud.</p><p>Meie Estoria meeskond on õnnelik teid iga sammu läbi saatma. Võtke meiega ühendust tasuta esialgse konsultatsiooni saamiseks.</p>",
                    MetaTitle       = "Juhend oma esimese kodu ostmiseks Eestis | Estoria",
                    MetaDescription = "Kõik, mida esmakordsetel ostjatel on vaja teada kinnisvara ostmiseks Eestis — kulud, protsess, notar ja näpunäited Estoria maakleritelt."
                },
                new BlogPostTranslation
                {
                    Language        = Language.Ru,
                    Title           = "Полное руководство по покупке первого жилья в Эстонии",
                    Excerpt         = "Покупка недвижимости в Эстонии — прозрачный и безопасный процесс, но есть важные шаги, затраты и подводные камни, которые должен понимать каждый покупатель первого жилья.",
                    Content         = "<p>Эстония имеет одну из наиболее прозрачных систем сделок с недвижимостью в Европе, основанную на полностью цифровом реестре недвижимости и надёжной нотариальной системе. Для покупателя первого жилья понимание процесса лишает ореола загадочности то, что иначе может казаться сложным.</p><h2>Шаг 1: Бюджет и финансирование</h2><p>Начните с понимания вашего общего бюджета — не только цены покупки, но и всех сопутствующих расходов. В Эстонии они обычно включают нотариальные сборы (0,3–0,5% от стоимости сделки), государственные регистрационные сборы (0,4%), налог на передачу для нерезидентов и юридические сборы при обращении к адвокату. Банковское финансирование широко доступно; большинство кредиторов требуют первоначальный взнос 15–20% для основного жилья.</p><h2>Шаг 2: Выбор правильного района</h2><p>Каждый район Таллина имеет свой характер, ценовую категорию и стиль жизни. Кесклинн предлагает городское удобство по премиальным ценам; Каламая и Пыхья-Таллинн — яркие и художественные; Кадриорг и Пирита — более зелёные и ориентированные на семью; Нымме — самый тихий и деревенский. Учтите ваш ежедневный маршрут, школьные районы и желаемый образ жизни.</p><h2>Шаг 3: Договор купли-продажи</h2><p>В Эстонии сделки с недвижимостью должны быть нотариально удостоверены. После согласования цены вы подписываете предварительный договор и вносите задаток — обычно 10%. Нотариальный акт подписывается через четыре-восемь недель. Нотариус — независимый государственный чиновник, который проверяет личность, подробно объясняет документ и гарантирует соответствие сделки законодательству.</p><h2>Шаг 4: Регистрация и передача</h2><p>После подписания нотариус регистрирует нового владельца в реестре недвижимости — часто в тот же день. Ключи передаются в согласованную дату, и сделка завершена.</p><p>Наша команда Estoria рада сопроводить вас на каждом этапе. Свяжитесь с нами для бесплатной первичной консультации.</p>",
                    MetaTitle       = "Руководство по покупке первого жилья в Эстонии | Estoria",
                    MetaDescription = "Всё, что нужно знать покупателям первого жилья о приобретении недвижимости в Эстонии — расходы, процесс, нотариус и советы от агентов Estoria."
                },
            ]
        };

        var blog3 = new BlogPost
        {
            Slug          = "best-neighbourhoods-tallinn",
            CoverImageUrl = "https://placehold.co/1200x630?text=Tallinn+Neighbourhoods",
            AuthorId      = agent2.Id,
            Status        = BlogPostStatus.Published,
            PublishedAt   = now.AddDays(-45),
            Translations  =
            [
                new BlogPostTranslation
                {
                    Language        = Language.En,
                    Title           = "The Best Neighbourhoods in Tallinn: A 2025 Guide",
                    Excerpt         = "From the bohemian streets of Kalamaja to the grand boulevards of Kadriorg, each Tallinn neighbourhood offers a completely different way of living.",
                    Content         = "<p>Tallinn is a city of contrasts — medieval towers stand alongside glass skyscrapers, Soviet-era housing blocks border Art Nouveau villas. Understanding the character of each neighbourhood is essential before committing to a purchase or long-term rental.</p><h2>Kesklinn — The City Centre</h2><p>Kesklinn is Tallinn's commercial and cultural heart. Home to the main department stores, theatres, the Estonian National Opera and a growing number of international restaurants, it attracts professionals who value walkability above all else. Apartment prices here are the highest in the city — typically €4 000–€6 500/m² — but so is the convenience.</p><h2>Kalamaja — The Creative Quarter</h2><p>Once a working-class fishing district, Kalamaja reinvented itself from the early 2010s as a centre for design studios, independent coffee shops, galleries and co-working spaces. The architectural mix of 19th-century timber houses and tastefully converted industrial buildings gives the area a uniquely authentic feel. Prices range from €3 200–€4 800/m², representing excellent value for the quality of life on offer.</p><h2>Kadriorg — Elegance and Green Space</h2><p>Kadriorg is Tallinn's answer to the sought-after tree-lined residential districts found in other European capitals. The neighbourhood is anchored by Kadriorg Park, the Presidential Palace and the KUMU Art Museum. Families favour it for its excellent schools, peaceful streets and proximity to the sea. Prices: €3 500–€5 500/m².</p><h2>Pirita — Coastal Living</h2><p>Pirita is the closest thing to a beach suburb in Tallinn. The sandy coastline, Pirita River and Olympic Sailing Centre give the area a relaxed, active feel. Most properties here are detached houses or low-rise apartments; it is one of the few places in Tallinn where you can own a detached house with a garden at a reasonable price relative to the lifestyle offered.</p><h2>Nõmme — The Garden City</h2><p>Nõmme is beloved by those who want the calm of suburban life without fully leaving the city. Its forest paths, allotment gardens and quiet streets attract families, retirees and remote workers. House prices start around €250 000 for a period property requiring some work — making it one of the more accessible entry points to Tallinn homeownership.</p>",
                    MetaTitle       = "Best Neighbourhoods in Tallinn 2025 | Estoria",
                    MetaDescription = "A comprehensive guide to Tallinn's best neighbourhoods — prices, lifestyle and what makes each area unique, from Estoria's local experts."
                },
                new BlogPostTranslation
                {
                    Language        = Language.Et,
                    Title           = "Parimad linnaosad Tallinnas: 2025. aasta juhend",
                    Excerpt         = "Kalamaja boheemlastest tänavatelt Kadrioru suurejooneliste puiesteede kaudu — igal Tallinna linnaosa pakub täiesti erinevat eluviisi.",
                    Content         = "<p>Tallinn on kontrastide linn — keskaegne tornid seisavad kõrvuti klaas-pilvelõhkujatega, Nõukogude-aegsed elamumajad piiravad juugendstiiliga villadega. Iga linnaosa iseloomu mõistmine on oluline enne ostu- või pikaajalise üürimiskohustuse võtmist.</p><h2>Kesklinn — Linnakeskus</h2><p>Kesklinn on Tallinna kaubanduslik ja kultuuriline süda. Peamised kaubanduskeskused, teatrid, Eesti Rahvusooper ja kasvav arv rahvusvahelisi restorane köidavad professionaale, kes hindavad jalutuskaugust kõige rohkem. Korterite hinnad on siin linnas kõrgeimad — tavaliselt 4 000–6 500 €/m² — kuid sama kõrge on mugavus.</p><h2>Kalamaja — Loomekvartali</h2><p>Kunagine tööliskalureioon leiutas end 2010. aastate algusest uuesti disainistuudiote, iseseisvate kohvikute, galeriide ja koostöistruumide keskusena. 19. sajandi puumajade ja maitsekalt ümber ehitatud tööstushoonete arhitektuurne segu annab piirkonnale ainulaadselt autentse tunde. Hinnad vahemikus 3 200–4 800 €/m², esindades suurepärast väärtust pakutava elukvaliteedi jaoks.</p><h2>Kadriorg — Elegants ja rohealad</h2><p>Kadriorg on Tallinna vastus teistes Euroopa pealinnades leitud nõutud puiesteedega elurajooni. Linnaosa on ankurdatud Kadrioru parki, Presidendipalee ja KUMU Kunstimuuseumiga. Perekonnad eelistavad seda oma suurepäraste koolide, rahulike tänavate ja mere läheduse tõttu. Hinnad: 3 500–5 500 €/m².</p><h2>Pirita — Rannikuelu</h2><p>Pirita on Tallinnas rannapredreedi lähedaim asi. Liivane rannajoone, Pirita jõe ja Olümpia purjelaudade keskuse tõttu on piirkonnal lõõgastuv, aktiivne tunne. Enamik kinnisvara siin on eramajad või madalmajad; see on üks väheseid kohti Tallinnas, kus saab omada eramaja aiaga mõistliku hinnaga pakutava elustiili suhtes.</p><h2>Nõmme — Aiakesklinn</h2><p>Nõmmet armastavad need, kes soovivad linnaääre elu rahulikkust linna täielikult lahkumata. Selle metsakäigud, aiamaalapid ja vaiksed tänavad köidavad peresid, pensionäre ja kaugtöötajaid. Majahinnad algavad umbes 250 000 eurolt mõnda tööd vajava ajaloolise kinnisvara jaoks — muutes selle üheks Tallinna omanikuks saamise ligipääsetavamaks sisenemispunktiks.</p>",
                    MetaTitle       = "Parimad linnaosad Tallinnas 2025 | Estoria",
                    MetaDescription = "Põhjalik juhend Tallinna parimatele linnaosadele — hinnad, elustiil ja mis teeb iga piirkonna ainulaadseks, Estoria kohalikelt ekspertidelt."
                },
                new BlogPostTranslation
                {
                    Language        = Language.Ru,
                    Title           = "Лучшие районы Таллина: путеводитель 2025 года",
                    Excerpt         = "От богемных улиц Каламая до величественных бульваров Кадриорга — каждый район Таллина предлагает совершенно иной образ жизни.",
                    Content         = "<p>Таллин — город контрастов: средневековые башни стоят рядом со стеклянными небоскрёбами, советские жилые кварталы граничат с виллами в стиле модерн. Понимание характера каждого района необходимо перед принятием решения о покупке или долгосрочной аренде.</p><h2>Кесклинн — Городской центр</h2><p>Кесклинн — коммерческое и культурное сердце Таллина. Главные торговые центры, театры, Эстонская Национальная Опера и растущее число международных ресторанов привлекают профессионалов, ценящих прежде всего пешую доступность. Цены на квартиры здесь самые высокие в городе — обычно 4 000–6 500 €/м² — но и удобство соответствующее.</p><h2>Каламая — Творческий квартал</h2><p>Когда-то рабочий рыбацкий район, Каламая заново изобрёл себя с начала 2010-х годов как центр дизайнерских студий, независимых кофеен, галерей и коворкинг-пространств. Архитектурное сочетание деревянных домов 19 века и со вкусом переоборудованных промышленных зданий придаёт району неповторимо аутентичное ощущение. Цены: 3 200–4 800 €/м² — отличное соотношение цены и качества жизни.</p><h2>Кадриорг — Элегантность и зелёные пространства</h2><p>Кадриорг — таллинский ответ на востребованные озеленённые жилые кварталы других европейских столиц. Район опирается на Кадриоргский парк, Президентский дворец и Художественный музей KUMU. Семьи предпочитают его за отличные школы, тихие улицы и близость к морю. Цены: 3 500–5 500 €/м².</p><h2>Пирита — Прибрежная жизнь</h2><p>Пирита — ближайший аналог пляжного пригорода в Таллине. Песчаное побережье, река Пирита и Олимпийский центр парусного спорта создают расслабленную, активную атмосферу. Большинство объектов здесь — отдельные дома или малоэтажные квартиры; это одно из немногих мест в Таллине, где можно владеть отдельным домом с садом по разумной цене относительно предлагаемого образа жизни.</p><h2>Нымме — Город-сад</h2><p>Нымме любят те, кто хочет спокойствия пригородной жизни, не покидая полностью город. Лесные тропы, огородные участки и тихие улицы привлекают семьи, пенсионеров и удалённых работников. Цены на дома начинаются от 250 000 € за исторический объект, требующий небольшого ремонта, — что делает его одной из более доступных точек входа в жильё Таллина.</p>",
                    MetaTitle       = "Лучшие районы Таллина 2025 | Estoria",
                    MetaDescription = "Исчерпывающий путеводитель по лучшим районам Таллина — цены, образ жизни и что делает каждый район уникальным, от местных экспертов Estoria."
                },
            ]
        };

        _db.BlogPosts.AddRange(blog1, blog2, blog3);

        // ── 6. Career Postings ────────────────────────────────────────────────

        var careers = new List<CareerPosting>
        {
            new CareerPosting
            {
                Slug     = "real-estate-agent",
                IsActive = true,
                Translations =
                [
                    new CareerPostingTranslation
                    {
                        Language    = Language.En,
                        Title       = "Real Estate Agent",
                        Location    = "Tallinn, Estonia",
                        Description = "We are looking for a motivated and client-focused Real Estate Agent to join our growing team in Tallinn. You will manage a portfolio of residential listings, conduct property viewings, negotiate offers and guide clients from initial enquiry through to notarial deed.\n\nRequirements: Valid Estonian real estate agent licence (maaklerilitsents), excellent communication skills in Estonian and English, local market knowledge, and a track record of customer satisfaction. Fluency in Russian is an advantage.\n\nWe offer a competitive commission structure, full administrative and marketing support, a modern CRM system and continuous professional development. If you are passionate about property and people, we would love to hear from you."
                    },
                    new CareerPostingTranslation
                    {
                        Language    = Language.Et,
                        Title       = "Kinnisvaramaakler",
                        Location    = "Tallinn, Eesti",
                        Description = "Otsime motiveeritud ja kliendikeskset kinnisvaramaaklert, et liituda meie kasvava meeskonnaga Tallinnas. Haldate elamupinnade portfelli, viite läbi kinnisvaraülevaatusi, läbiräägite pakkumisi ja juhendate kliente esialgsest päringust kuni notariaalsete dokumentideni.\n\nNõuded: Kehtiv Eesti kinnisvaramaaklerilitsents, suurepärased suhtlemisoskused eesti ja inglise keeles, kohalikud turuteadmised ja klienditeeninduse kogemus. Vene keele valdamine on eeliseks.\n\nPakume konkurentsivõimelist vahendustasude struktuuri, täielikku halduslikku ja turunduslikku tuge, kaasaegset CRM-süsteemi ja pidevat professionaalset arengut. Kui olete kirglik kinnisvara ja inimeste suhtes, kuulame hea meelega teie juurest."
                    },
                    new CareerPostingTranslation
                    {
                        Language    = Language.Ru,
                        Title       = "Агент по недвижимости",
                        Location    = "Таллин, Эстония",
                        Description = "Мы ищем мотивированного и ориентированного на клиентов агента по недвижимости для присоединения к нашей растущей команде в Таллине. Вы будете управлять портфелем жилых объявлений, проводить просмотры недвижимости, вести переговоры по предложениям и сопровождать клиентов от первоначального запроса до нотариального акта.\n\nТребования: Действующая эстонская лицензия агента по недвижимости, отличные коммуникативные навыки на эстонском и английском языках, знание местного рынка и опыт работы с клиентами. Знание русского языка является преимуществом.\n\nМы предлагаем конкурентоспособную структуру комиссионных, полную административную и маркетинговую поддержку, современную CRM-систему и непрерывное профессиональное развитие."
                    },
                ]
            },
            new CareerPosting
            {
                Slug     = "marketing-specialist",
                IsActive = true,
                Translations =
                [
                    new CareerPostingTranslation
                    {
                        Language    = Language.En,
                        Title       = "Marketing Specialist",
                        Location    = "Tallinn, Estonia (Hybrid)",
                        Description = "Estoria is seeking a creative and data-driven Marketing Specialist to own our digital presence across channels. You will plan and execute campaigns across social media (Instagram, Facebook, LinkedIn), manage our property listing portals, produce compelling written and visual content, and track performance metrics.\n\nRequirements: 2+ years of experience in digital marketing, proficiency with Meta Ads Manager and Google Analytics, strong copywriting skills in Estonian and English, and experience with Canva or Adobe Creative Suite. Knowledge of the real estate industry is a plus.\n\nThis is a hybrid role based primarily in our Tallinn office, with flexibility for remote work. We offer a competitive salary, creative freedom and the opportunity to shape the brand of one of Tallinn's most respected real estate agencies."
                    },
                    new CareerPostingTranslation
                    {
                        Language    = Language.Et,
                        Title       = "Turundusspetsialist",
                        Location    = "Tallinn, Eesti (Hübriid)",
                        Description = "Estoria otsib loomingulist ja andmepõhist turundusspetsialisti, kes vastutab meie digitaalse kohaloleku eest kanalite lõikes. Planeerite ja viite ellu kampaaniaid sotsiaalmeedia kanalites (Instagram, Facebook, LinkedIn), haldate meie kinnisvarakuulutuste portaale, toodate köitvat kirjalikku ja visuaalset sisu ning jälgite tulemusnäitajaid.\n\nNõuded: 2+ aastat kogemust digiturunduses, Meta Ads Manageri ja Google Analyticsi kasutamise oskus, tugevad tekstikirjutamise oskused eesti ja inglise keeles ning kogemus Canva või Adobe Creative Suite'iga. Kinnisvaratööstuse tundmine on pluss.\n\nSee on hübriidrooll, mis asub peamiselt meie Tallinna kontoris, koos paindlikkusega kaugtöö jaoks. Pakume konkurentsivõimelist palka, loomingulist vabadust ja võimalust kujundada ühe Tallinna austavaima kinnisvarabüroo brändi."
                    },
                    new CareerPostingTranslation
                    {
                        Language    = Language.Ru,
                        Title       = "Специалист по маркетингу",
                        Location    = "Таллин, Эстония (Гибридный формат)",
                        Description = "Estoria ищет творческого и основанного на данных специалиста по маркетингу для управления нашим цифровым присутствием по всем каналам. Вы будете планировать и реализовывать кампании в социальных сетях (Instagram, Facebook, LinkedIn), управлять нашими порталами объявлений о недвижимости, создавать убедительный письменный и визуальный контент, а также отслеживать показатели эффективности.\n\nТребования: 2+ года опыта в цифровом маркетинге, владение Meta Ads Manager и Google Analytics, сильные навыки написания текстов на эстонском и английском языках, опыт работы с Canva или Adobe Creative Suite. Знание отрасли недвижимости является плюсом.\n\nЭто гибридная роль, базирующаяся преимущественно в нашем таллинском офисе, с гибкостью для удалённой работы. Мы предлагаем конкурентоспособную зарплату, творческую свободу и возможность формировать бренд одного из самых уважаемых агентств недвижимости Таллина."
                    },
                ]
            },
        };

        _db.CareerPostings.AddRange(careers);

        await _db.SaveChangesAsync();
    }

    // ── Site Settings ─────────────────────────────────────────────────────────

    private async Task SeedSiteSettingsAsync()
    {
        var defaults = new (string Key, string? Value, SettingValueType Type)[]
        {
            // Public
            ("stats.years_experience",    "8",                                       SettingValueType.Number),
            ("stats.satisfaction_percent","98",                                      SettingValueType.Number),
            ("contact.email",             "info@estoria.estate",                     SettingValueType.Text),
            ("contact.phone",             "+372 600 1234",                           SettingValueType.Text),
            ("contact.address",           "Kotzebue 4, Tallinn 10412, Estonia",      SettingValueType.Text),
            ("contact.hours",             "Mon–Fri 09:00–18:00, Sat 10:00–15:00", SettingValueType.Text),
            ("social.facebook",           "",                                        SettingValueType.Text),
            ("social.instagram",          "",                                        SettingValueType.Text),
            ("social.linkedin",           "",                                        SettingValueType.Text),

            // Private (admin-only)
            ("watermark.enabled",         "true",                                    SettingValueType.Boolean),
            ("watermark.text",            "ESTORIA",                                 SettingValueType.Text),
            ("ai.descriptions_enabled",   "false",                                   SettingValueType.Boolean),
            ("ai.replies_enabled",        "false",                                   SettingValueType.Boolean),
            ("birthday.auto_send",        "false",                                   SettingValueType.Boolean),
            ("savedsearches.auto_send",   "false",                                   SettingValueType.Boolean),
        };

        var inserted = false;
        foreach (var (key, value, type) in defaults)
        {
            if (await _db.SiteSettings.AnyAsync(s => s.Key == key)) continue;

            _db.SiteSettings.Add(new SiteSetting
            {
                Key = key,
                Value = value,
                ValueType = type
            });
            inserted = true;
        }

        if (inserted)
            await _db.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PageContent PageContent(
        string key,
        IEnumerable<(Language lang, string? title, string? body, string? imageUrl, string? videoUrl)> translations)
    {
        var pc = new PageContent { PageKey = key };
        pc.Translations.AddRange(translations.Select(t => new PageContentTranslation
        {
            Language = t.lang,
            Title    = t.title,
            Body     = t.body,
            ImageUrl = t.imageUrl,
            VideoUrl = t.videoUrl
        }));
        return pc;
    }
}
