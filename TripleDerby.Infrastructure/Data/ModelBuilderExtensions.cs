using Microsoft.EntityFrameworkCore;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Infrastructure.Data;

public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        var adminId = new Guid("725C6768-7EAB-43B0-AA39-86F15E97824A");

        modelBuilder.Entity<User>().HasData(
             new User { Id = adminId, Username = "Breeders", Email = "breeders@triplederby.com", IsActive = true, IsAdmin = true },
             new User { Id = new Guid("72115894-88CD-433E-9892-CAC22E335F1D"), Username = "Racers", Email = "racers@triplederby.com", IsActive = true, IsAdmin = true },
             new User { Id = new Guid("1B4AB681-147F-4727-8923-33C6CA269878"), Username = "Player", Email = "player@triplederby.com", IsActive = true, IsAdmin = false }
         );

        modelBuilder.Entity<Color>().HasData(
            new Color { Id = 1, Weight = 1, IsSpecial = false, Name = "Gray", Description = "Characterized by progressive silvering of the colored hairs of the coat. Most gray horses have black skin and dark eyes; unlike many depigmentation genes, gray does not affect skin or eye color." },
            new Color { Id = 2, Weight = 1, IsSpecial = false, Name = "Bay", Description = "A reddish brown body color with a black mane, tail, ear edges, and lower legs. Bay is one of the most common coat colors in many horse breeds." },
            new Color { Id = 3, Weight = 1, IsSpecial = false, Name = "Seal Brown", Description = "A near-black body color; with black points, the mane, tail and legs; but also reddish or tan areas around the eyes, muzzle, behind the elbow and in front of the stifle." },
            new Color { Id = 4, Weight = 2, IsSpecial = false, Name = "Chestnut", Description = "Consisting of a reddish-to-brown coat with a mane and tail the same or lighter in color than the coat. Genetically and visually, chestnut is characterized by the absolute absence of true black hairs." },
            new Color { Id = 5, Weight = 3, IsSpecial = false, Name = "Black", Description = "Black is a relatively uncommon coat color, and novices frequently mistake dark chestnuts or bays for black." },
            new Color { Id = 6, Weight = 4, IsSpecial = false, Name = "Dapple Gray", Description = "A gray coat featuring lighter or darker circular spots, giving a mottled or dappled appearance across the body." },
            new Color { Id = 7, Weight = 5, IsSpecial = false, Name = "Roan", Description = "An even mixture of colored and white hairs on the body, while the head and 'points'—lower legs, mane and tail—are more solid-colored." },
            new Color { Id = 8, Weight = 6, IsSpecial = false, Name = "Liver Chestnut", Description = "A rich, dark reddish-brown chestnut color with a mane and tail of the same or slightly lighter shade." },
            new Color { Id = 9, Weight = 7, IsSpecial = false, Name = "Buckskin", Description = "A tan or gold coat with black mane, tail, and lower legs, resulting from a single cream gene on a bay base coat." },
            new Color { Id = 10, Weight = 8, IsSpecial = false, Name = "Cremello", Description = "A pale cream coat, pink skin, and blue eyes, the result of two cream dilution genes acting on a chestnut base." },
            new Color { Id = 11, Weight = 9, IsSpecial = false, Name = "Grullo", Description = "A smoky gray or blue-gray coat with black mane, tail, and lower legs, often featuring a distinctive dorsal stripe." },
            new Color { Id = 12, Weight = 10, IsSpecial = false, Name = "Champagne", Description = "A golden coat with metallic sheen and hazel or amber eyes, caused by the champagne dilution gene acting on chestnut or bay." },
            new Color { Id = 13, Weight = 12, IsSpecial = false, Name = "Palomino", Description = "A gold coat and white mane and tail. Genetically, the palomino color is created by a single allele of a dilution gene called the cream gene working on a 'red' (chestnut) base coat." },
            new Color { Id = 14, Weight = 50, IsSpecial = false, Name = "White", Description = "White horses are born white and stay white throughout their life. White horses may have brown, blue, or hazel eyes. 'True white' horses, especially those that carry one of the dominant white (W) genes, are rare." },
            new Color { Id = 15, Weight = 100, IsSpecial = false, Name = "Platinum", Description = "A silver color, more rich than dull gray, with a shimmering metallic tone that stands out in sunlight." },
            new Color { Id = 16, Weight = 500, IsSpecial = true, Name = "Pinto", Description = "A pinto horse has a coat color that consists of large patches of white and any other color." },
            new Color { Id = 17, Weight = 1_000, IsSpecial = true, Name = "Appaloosa", Description = "The Appaloosa is a horse breed best known for its colorful leopard-spotted coat pattern." },
            new Color { Id = 18, Weight = 5_000, IsSpecial = true, Name = "Holstein", Description = "A unique coat pattern resembling that of a Holstein cow—large black or brown patches on a white background." },
            new Color { Id = 19, Weight = 10_000, IsSpecial = true, Name = "Przewalski", Description = "A rare and endangered subspecies of wild horse, at one time extinct in the wild. It has been reintroduced to its native habitat in Mongolia at the Khustain Nuruu National Park, Takhin Tal Nature Reserve, and Khomiin Tal." },
            new Color { Id = 20, Weight = 15_000, IsSpecial = true, Name = "Zebra", Description = "An odd color combination that resembles that of a zebra, with distinct black and white stripes across the body." },
            new Color { Id = 21, Weight = 20_000, IsSpecial = true, Name = "Okapi", Description = "Stripes on the hindquarters resembling that of a zebra, with the brown coloring of a horse elsewhere." },
            new Color { Id = 22, Weight = 25_000, IsSpecial = true, Name = "Bengal", Description = "An odd color combination that resembles that of a tiger, with bold orange and black striping across the coat." },
            new Color { Id = 23, Weight = 30_000, IsSpecial = true, Name = "Panda", Description = "An odd color combination that resembles that of a panda, featuring black and white patches centered around the legs, shoulders, and eyes." },
            new Color { Id = 24, Weight = 100_000, IsSpecial = true, Name = "Unicorn", Description = "Extremely rare white horse with what appears to be a magical horn protruding from the center of the head." },
            new Color { Id = 25, Weight = 200_000, IsSpecial = true, Name = "Pegasus", Description = "Extremely rare horse with what appear to be large, majestic wings coming from the midsection." }
        );

        modelBuilder.Entity<Statistic>().HasData(
            new Statistic { Id = StatisticId.Speed, Name = "Speed", Description = "Primary stat affecting race performance. Higher speed = faster base pace. Multiplier: ±10% at extremes.", IsGenetic = true },
            new Statistic { Id = StatisticId.Stamina, Name = "Stamina", Description = "Determines stamina pool size and depletion rate. Critical for longer races. Higher stamina = slower depletion.", IsGenetic = true },
            new Statistic { Id = StatisticId.Agility, Name = "Agility", Description = "Secondary stat affecting maneuverability and positioning. Provides moderate speed boost. Multiplier: ±5% at extremes.", IsGenetic = true },
            new Statistic { Id = StatisticId.Durability, Name = "Durability", Description = "Affects stamina efficiency during races. High durability = fuel-efficient, slower stamina burn. Multiplier: ±15% depletion rate.", IsGenetic = true },
            new Statistic { Id = StatisticId.Happiness, Name = "Happiness", Description = "Current mood and well-being. Affected by feeding, training, and racing. Resets monthly. Influences training effectiveness.", IsGenetic = false }
        );

        modelBuilder.Entity<Feeding>().HasData(
            new Feeding { Id = 1, Name = "Apple", Description = "Crisp and sweet treat. Increases happiness moderately." },
            new Feeding { Id = 2, Name = "Carrot", Description = "Crunchy and nutritious vegetable. Boosts happiness and provides minor stat benefits." },
            new Feeding { Id = 3, Name = "Oats", Description = "High-energy grain feed. Provides sustained energy and moderate happiness increase." },
            new Feeding { Id = 4, Name = "Sugar Cube", Description = "Sweet indulgence. Significantly increases happiness but provides little nutritional value." },
            new Feeding { Id = 5, Name = "Hay", Description = "Basic roughage and fiber. Maintains baseline happiness and digestive health." },
            new Feeding { Id = 6, Name = "Peppermint", Description = "Refreshing herbal treat. Calming effect that moderately increases happiness." }
        );

        modelBuilder.Entity<Training>().HasData(
            new Training { Id = 1, Name = "Sprint", Description = "Short-distance speed work. Focuses on explosive acceleration and top speed development." },
            new Training { Id = 2, Name = "Endurance Run", Description = "Long-distance conditioning. Builds stamina and cardiovascular fitness for extended races." },
            new Training { Id = 3, Name = "Jump Training", Description = "Obstacle work developing power and coordination. Improves agility and muscle strength." },
            new Training { Id = 4, Name = "Hill Climbing", Description = "Incline training for leg strength. Builds power, stamina, and durability." },
            new Training { Id = 5, Name = "Obstacle Course", Description = "Complex navigation training. Enhances agility, decision-making, and overall athleticism." },
            new Training { Id = 6, Name = "Swimming", Description = "Low-impact conditioning. Builds endurance and muscle without joint strain, improves durability." }
        );

        modelBuilder.Entity<Condition>().HasData(
            new Condition { Id = ConditionId.Fast, Name = "Fast" },
            new Condition { Id = ConditionId.WetFast, Name = "Wet Fast" },
            new Condition { Id = ConditionId.Good, Name = "Good" },
            new Condition { Id = ConditionId.Muddy, Name = "Muddy" },
            new Condition { Id = ConditionId.Sloppy, Name = "Sloppy" },
            new Condition { Id = ConditionId.Frozen, Name = "Frozen" },
            new Condition { Id = ConditionId.Slow, Name = "Slow" },
            new Condition { Id = ConditionId.Heavy, Name = "Heavy" },
            new Condition { Id = ConditionId.Firm, Name = "Firm" },
            new Condition { Id = ConditionId.Soft, Name = "Soft" },
            new Condition { Id = ConditionId.Yielding, Name = "Yielding" }
        );

        modelBuilder.Entity<Surface>().HasData(
            new Surface { Id = SurfaceId.Dirt, Name = "Dirt" },
            new Surface { Id = SurfaceId.Turf, Name = "Turf" },
            new Surface { Id = SurfaceId.Artificial, Name = "Artificial" }
        );

        modelBuilder.Entity<Track>().HasData(
            new Track { Id = TrackId.TripleSpires, Name = "Triple Spires" },
            new Track { Id = TrackId.BellMeade, Name = "Belle Meade Park" },
            new Track { Id = TrackId.Pimento, Name = "Pimento Race Course" }
        );

        modelBuilder.Entity<LegType>().HasData(
            new LegType { Id = LegTypeId.FrontRunner, Name = "Front-runner", Description = "Gets 3% speed boost during first 20% of race." },
            new LegType { Id = LegTypeId.StartDash, Name = "Start Dash", Description = "Gets 4% speed boost during first 25% of race." },
            new LegType { Id = LegTypeId.LastSpurt, Name = "Last Spurt", Description = "Gets 4% speed boost during final 25% of race." },
            new LegType { Id = LegTypeId.StretchRunner, Name = "Stretch-runner", Description = "Gets 3% speed boost during 60-80% of race (stretch run)." },
            new LegType { Id = LegTypeId.RailRunner, Name = "Rail-runner", Description = "Gets 3% speed boost when in lane 1 with clear path ahead." }
        );

        modelBuilder.Entity<Race>().HasData(
            new Race { Id = 1, Name = "Triple Derby", Description = "Run for the Spires", SurfaceId = SurfaceId.Dirt, TrackId = TrackId.TripleSpires, Furlongs = 10 },
            new Race { Id = 2, Name = "Belle Meade Stakes", Description = "Race of Winners", SurfaceId = SurfaceId.Dirt, TrackId = TrackId.BellMeade, Furlongs = 12 },
            new Race { Id = 3, Name = "Freaky Stakes", Description = "Run for the Sunflowers", SurfaceId = SurfaceId.Dirt, TrackId = TrackId.Pimento, Furlongs = 9.5m }
        );

        // Sires
        modelBuilder.Entity<Horse>().HasData(
            new Horse { Id = new Guid("649A8C3F-A63D-485F-8809-8404C848BCA0"), Name = "Camarero", ColorId = 2, LegTypeId = LegTypeId.FrontRunner, IsMale = true, OwnerId = adminId, RaceStarts = 76, RaceWins = 73, RacePlace = 2, RaceShow = 0, Earnings = 43553, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("AB0E87B3-CDE6-4BD0-9BEF-AAF1C9A5AFB9"), Name = "Hindoo", ColorId = 2, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 35, RaceWins = 30, RacePlace = 3, RaceShow = 2, Earnings = 71875, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("05A042EF-0EE5-4660-B2A1-99E6B7D5D294"), Name = "El Rio Rey", ColorId = 2, LegTypeId = LegTypeId.RailRunner, IsMale = true, OwnerId = adminId, RaceStarts = 7, RaceWins = 7, RacePlace = 0, RaceShow = 0, Earnings = 46835, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("AD57472C-1D81-47D7-8618-1D9CE4B0A1B3"), Name = "Golden Fleece", ColorId = 2, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 4, RaceWins = 4, RacePlace = 0, RaceShow = 0, Earnings = 283967, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("2107FD5C-34CF-477F-9DB5-924FD34B22F9"), Name = "Seattle Slew", ColorId = 3, LegTypeId = LegTypeId.FrontRunner, IsMale = true, OwnerId = adminId, RaceStarts = 17, RaceWins = 14, RacePlace = 2, RaceShow = 0, Earnings = 1208726, IsRetired = true, Parented = 1, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("210BB356-F8AE-4DBE-98F6-16DFA20CF930"), Name = "War Admiral", ColorId = 3, LegTypeId = LegTypeId.RailRunner, IsMale = true, OwnerId = adminId, RaceStarts = 26, RaceWins = 21, RacePlace = 3, RaceShow = 1, Earnings = 273240, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("D1F1A1C6-885B-4D27-9375-572C7D42EF32"), Name = "Secretariat", ColorId = 4, LegTypeId = LegTypeId.FrontRunner, IsMale = true, OwnerId = adminId, RaceStarts = 21, RaceWins = 16, RacePlace = 3, RaceShow = 1, Earnings = 167513, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("B10F1F36-39F2-4F91-AE43-B727ED1D9FF8"), Name = "Man o' War", ColorId = 4, LegTypeId = LegTypeId.FrontRunner, IsMale = true, OwnerId = adminId, RaceStarts = 21, RaceWins = 20, RacePlace = 0, RaceShow = 0, Earnings = 249465, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("3E7FE0A3-DA6E-4F08-B12E-48D8D7B1F6DA"), Name = "Citation", ColorId = 2, LegTypeId = LegTypeId.StartDash, IsMale = true, OwnerId = adminId, RaceStarts = 45, RaceWins = 32, RacePlace = 10, RaceShow = 2, Earnings = 1085760, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("A1D5E6F4-3D6B-46BC-A89F-072BF74A6BA8"), Name = "American Pharoah", ColorId = 4, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 11, RaceWins = 9, RacePlace = 1, RaceShow = 0, Earnings = 8650300, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("7F6B85E9-82A5-4C7C-89D6-BE1F0AB4C8E4"), Name = "Justify", ColorId = 5, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 6, RaceWins = 6, RacePlace = 0, RaceShow = 0, Earnings = 3798000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("B16FB2C8-519E-4C72-9E39-F586B42A4C6B"), Name = "Affirmed", ColorId = 4, LegTypeId = LegTypeId.RailRunner, IsMale = true, OwnerId = adminId, RaceStarts = 29, RaceWins = 22, RacePlace = 5, RaceShow = 1, Earnings = 2393818, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("B2F8BA89-073B-48F3-9C9E-1EFBED6D093E"), Name = "Alydar", ColorId = 4, LegTypeId = LegTypeId.FrontRunner, IsMale = true, OwnerId = adminId, RaceStarts = 26, RaceWins = 14, RacePlace = 9, RaceShow = 1, Earnings = 957195, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("47ED5B3D-0ED4-4A1B-BF1B-2D2B09FF5E5C"), Name = "Whirlaway", ColorId = 4, LegTypeId = LegTypeId.StartDash, IsMale = true, OwnerId = adminId, RaceStarts = 60, RaceWins = 32, RacePlace = 15, RaceShow = 9, Earnings = 561161, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("EB601D48-CB8E-4B4F-8DFE-09F9F5F6B0C4"), Name = "Northern Dancer", ColorId = 1, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 18, RaceWins = 14, RacePlace = 2, RaceShow = 2, Earnings = 580647, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("6B7A5F42-1D3E-4E0F-AC1C-59D8E4E9E2E7"), Name = "Spectacular Bid", ColorId = 1, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 30, RaceWins = 26, RacePlace = 2, RaceShow = 1, Earnings = 2781608, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("C1A38B67-5A4C-4E87-9D18-3A4DA6E6A6F7"), Name = "Seabiscuit", ColorId = 3, LegTypeId = LegTypeId.RailRunner, IsMale = true, OwnerId = adminId, RaceStarts = 89, RaceWins = 33, RacePlace = 15, RaceShow = 13, Earnings = 437730, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("B8F9D7B2-9A35-4B4F-8F7C-56F8E2E9E8E3"), Name = "Kelso", ColorId = 3, LegTypeId = LegTypeId.FrontRunner, IsMale = true, OwnerId = adminId, RaceStarts = 63, RaceWins = 39, RacePlace = 12, RaceShow = 2, Earnings = 1977896, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("A3F2C1B8-9B57-4D3A-8E2C-1F7D8E4E9E2B"), Name = "Easy Goer", ColorId = 3, LegTypeId = LegTypeId.StartDash, IsMale = true, OwnerId = adminId, RaceStarts = 20, RaceWins = 14, RacePlace = 5, RaceShow = 1, Earnings = 4873770, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("5A6F7E3D-1D3A-4B6F-9C9E-8E7E6E6E2E7D"), Name = "Dr. Fager", ColorId = 1, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 22, RaceWins = 18, RacePlace = 2, RaceShow = 1, Earnings = 1002642, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }
        );
        modelBuilder.Entity<Horse>().HasData(
            new Horse { Id = new Guid("b8a4f3c5-6d9c-4d8c-9f0b-7c9d1b3a5f21"), Name = "Bold Ruler", ColorId = 2, LegTypeId = LegTypeId.FrontRunner, IsMale = true, OwnerId = adminId, RaceStarts = 33, RaceWins = 23, RacePlace = 4, RaceShow = 2, Earnings = 764204, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("b1b3c9d2-3e80-4f0d-9a78-1a2f33e7c4a9"), Name = "Buckpasser", ColorId = 2, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 31, RaceWins = 25, RacePlace = 4, RaceShow = 1, Earnings = 1462014, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("4c5f2e8a-1f0b-4bde-9a40-3b1c9a6d7e22"), Name = "Mr. Prospector", ColorId = 2, LegTypeId = LegTypeId.StartDash, IsMale = true, OwnerId = adminId, RaceStarts = 14, RaceWins = 7, RacePlace = 4, RaceShow = 2, Earnings = 112171, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("0e7a2c91-7c5a-4a54-9b1f-2f7d9c3e1a88"), Name = "Storm Cat", ColorId = 3, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 8, RaceWins = 4, RacePlace = 3, RaceShow = 0, Earnings = 570610, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("9f2d6a1b-8c34-4d77-9a1c-5e0f4c2b7d11"), Name = "A.P. Indy", ColorId = 2, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 11, RaceWins = 8, RacePlace = 0, RaceShow = 1, Earnings = 2979815, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("a6d4c3f1-2b7e-4a9c-8e5d-1f2c3a4b5e66"), Name = "Danzig", ColorId = 2, LegTypeId = LegTypeId.StartDash, IsMale = true, OwnerId = adminId, RaceStarts = 3, RaceWins = 3, RacePlace = 0, RaceShow = 0, Earnings = 32400, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("c4e1b2a7-97d0-4a2a-9a6c-7d1e2f3c4b55"), Name = "Fappiano", ColorId = 3, LegTypeId = LegTypeId.RailRunner, IsMale = true, OwnerId = adminId, RaceStarts = 17, RaceWins = 10, RacePlace = 3, RaceShow = 1, Earnings = 1106640, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("7b3a9c1e-5f84-4cfd-9ad2-3e1b7f0c2a40"), Name = "Unbridled", ColorId = 2, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 24, RaceWins = 8, RacePlace = 6, RaceShow = 6, Earnings = 4489475, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("5e2a7d9c-3b1f-4f0e-8a6d-1c2b3a4e5f77"), Name = "Distorted Humor", ColorId = 4, LegTypeId = LegTypeId.StartDash, IsMale = true, OwnerId = adminId, RaceStarts = 23, RaceWins = 8, RacePlace = 5, RaceShow = 3, Earnings = 769964, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("2f6b1c7a-8e9d-4a3c-9b1e-5a7d2c4f0e11"), Name = "Smart Strike", ColorId = 2, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 8, RaceWins = 6, RacePlace = 1, RaceShow = 0, Earnings = 337376, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("d7e4a2b1-6c3f-4a9e-8b2d-1f0e7c5a3b66"), Name = "Medaglia d'Oro", ColorId = 3, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 17, RaceWins = 8, RacePlace = 7, RaceShow = 0, Earnings = 5754720, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("1a9c3e7f-2b5d-47a6-9c1e-8d0f2a3b4c55"), Name = "Bernardini", ColorId = 2, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 8, RaceWins = 6, RacePlace = 1, RaceShow = 0, Earnings = 3060480, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("6c2b7e1a-9f0d-4a3b-8c5e-1d2f3a4b5c88"), Name = "Street Cry", ColorId = 2, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 12, RaceWins = 5, RacePlace = 6, RaceShow = 1, Earnings = 5303675, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("f0a3b2c1-6e7d-4d9a-8c1b-2a5f3e7c4b90"), Name = "Empire Maker", ColorId = 3, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 8, RaceWins = 4, RacePlace = 3, RaceShow = 1, Earnings = 1985800, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("3b7e1c5a-2f0d-4a9c-8e3b-1d6f2a4c5e22"), Name = "Mineshaft", ColorId = 3, LegTypeId = LegTypeId.RailRunner, IsMale = true, OwnerId = adminId, RaceStarts = 18, RaceWins = 10, RacePlace = 3, RaceShow = 2, Earnings = 2283402, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("e6a1d2f3-7c4b-4e9a-9b0d-2f5a3c7e1b44"), Name = "Pioneerof the Nile", ColorId = 3, LegTypeId = LegTypeId.StretchRunner, IsMale = true, OwnerId = adminId, RaceStarts = 10, RaceWins = 5, RacePlace = 1, RaceShow = 1, Earnings = 1634200, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("9c2f1e7a-3b5d-4a8c-9d0e-1f6a2c4b5e77"), Name = "Quality Road", ColorId = 2, LegTypeId = LegTypeId.FrontRunner, IsMale = true, OwnerId = adminId, RaceStarts = 13, RaceWins = 8, RacePlace = 3, RaceShow = 1, Earnings = 2232830, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("0b7e3c2a-5f1d-4a9c-8e6b-2d3f1a4c5e19"), Name = "Speightstown", ColorId = 4, LegTypeId = LegTypeId.StartDash, IsMale = true, OwnerId = adminId, RaceStarts = 16, RaceWins = 10, RacePlace = 2, RaceShow = 2, Earnings = 1258256, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("a2e7b5c1-4d9f-4fa0-9c1e-7b3a6d2f0c66"), Name = "Tapit", ColorId = 1, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 6, RaceWins = 3, RacePlace = 0, RaceShow = 1, Earnings = 557300, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("7e1c3b5a-2f9d-4a0c-8d6e-1a2f4c5b7e33"), Name = "Curlin", ColorId = 4, LegTypeId = LegTypeId.LastSpurt, IsMale = true, OwnerId = adminId, RaceStarts = 16, RaceWins = 11, RacePlace = 2, RaceShow = 2, Earnings = 10501800, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }
        );

        // Dams
        modelBuilder.Entity<Horse>().HasData(
            new Horse { Id = new Guid("F9D3BA4D-26C4-4DAC-87DF-506FF3F8EB87"), Name = "Peppers Pride", ColorId = 3, LegTypeId = LegTypeId.LastSpurt, IsMale = false, OwnerId = adminId, RaceStarts = 19, RaceWins = 19, RacePlace = 0, RaceShow = 0, Earnings = 1066085, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("39D0F225-0B23-4753-84AE-5394011DF403"), Name = "Madelia", ColorId = 4, LegTypeId = LegTypeId.StartDash, IsMale = false, OwnerId = adminId, RaceStarts = 4, RaceWins = 4, RacePlace = 0, RaceShow = 0, Earnings = 1385000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("4FD18871-59D3-4E9E-A519-67D7CCCC70FA"), Name = "Personal Ensign", ColorId = 2, LegTypeId = LegTypeId.RailRunner, IsMale = false, OwnerId = adminId, RaceStarts = 13, RaceWins = 13, RacePlace = 0, RaceShow = 0, Earnings = 1679880, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("E907D1D1-FDF8-446C-B11A-DF97C093AAF9"), Name = "Landaluce", ColorId = 3, LegTypeId = LegTypeId.StartDash, IsMale = false, OwnerId = adminId, SireId = new Guid("2107FD5C-34CF-477F-9DB5-924FD34B22F9"), RaceStarts = 5, RaceWins = 5, RacePlace = 0, RaceShow = 0, Earnings = 372365, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("A18A61FA-18E7-4C2B-8A8B-1D9F2F3E3DB1"), Name = "Ruffian", ColorId = 1, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 11, RaceWins = 10, RacePlace = 0, RaceShow = 0, Earnings = 313428, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("639B2D9E-33E3-41DA-9B9F-936A97E59F77"), Name = "Zenyatta", ColorId = 3, LegTypeId = LegTypeId.StartDash, IsMale = false, OwnerId = adminId, RaceStarts = 20, RaceWins = 19, RacePlace = 1, RaceShow = 0, Earnings = 7285000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("60A9B06A-B1A6-4844-AE8C-B52E2D785540"), Name = "Rachel Alexandra", ColorId = 4, LegTypeId = LegTypeId.LastSpurt, IsMale = false, OwnerId = adminId, RaceStarts = 19, RaceWins = 13, RacePlace = 5, RaceShow = 0, Earnings = 3461840, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("1A737DBC-89CE-4C6C-A833-9F76B2D75998"), Name = "Lady's Secret", ColorId = 2, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 45, RaceWins = 25, RacePlace = 9, RaceShow = 3, Earnings = 3014515, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("FB9D4345-6D6E-4C5E-9AC7-C8E1A3C5A3FA"), Name = "Miesque", ColorId = 3, LegTypeId = LegTypeId.RailRunner, IsMale = false, OwnerId = adminId, RaceStarts = 16, RaceWins = 12, RacePlace = 3, RaceShow = 0, Earnings = 2201931, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("8D52A8A3-1339-4B49-AE8A-AC2CFA723B5C"), Name = "Goldikova", ColorId = 4, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 27, RaceWins = 17, RacePlace = 6, RaceShow = 0, Earnings = 7442561, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("3917B927-E776-4DDB-9193-9F19E0DC84D0"), Name = "Dahlia", ColorId = 1, LegTypeId = LegTypeId.StartDash, IsMale = false, OwnerId = adminId, RaceStarts = 48, RaceWins = 15, RacePlace = 3, RaceShow = 4, Earnings = 1429192, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("BFC7B007-6D6C-4723-933B-9FC58D4A1B50"), Name = "Black Caviar", ColorId = 2, LegTypeId = LegTypeId.LastSpurt, IsMale = false, OwnerId = adminId, RaceStarts = 25, RaceWins = 25, RacePlace = 0, RaceShow = 0, Earnings = 7940735, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("D4695321-5F97-4D37-BE34-8E6D50E47E4D"), Name = "Winx", ColorId = 3, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 43, RaceWins = 37, RacePlace = 3, RaceShow = 0, Earnings = 26759644, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("947C95DE-C0C8-468C-B6DF-6A54228A16F1"), Name = "Songbird", ColorId = 4, LegTypeId = LegTypeId.RailRunner, IsMale = false, OwnerId = adminId, RaceStarts = 15, RaceWins = 13, RacePlace = 2, RaceShow = 0, Earnings = 4420000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("F0EC7B9A-5567-4E5C-8D48-8A6E354B8C8A"), Name = "Enable", ColorId = 1, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 19, RaceWins = 15, RacePlace = 2, RaceShow = 0, Earnings = 14300000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("D6A71C90-1298-4938-8F8F-128FC4C8F10D"), Name = "Beholder", ColorId = 2, LegTypeId = LegTypeId.StartDash, IsMale = false, OwnerId = adminId, RaceStarts = 26, RaceWins = 18, RacePlace = 6, RaceShow = 0, Earnings = 6480692, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("D8E2AA39-5D84-4D37-AF45-92497BBE0C08"), Name = "Forever Together", ColorId = 3, LegTypeId = LegTypeId.LastSpurt, IsMale = false, OwnerId = adminId, RaceStarts = 14, RaceWins = 5, RacePlace = 3, RaceShow = 3, Earnings = 2912014, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("9ACFAE7F-60F1-420C-813B-C3E3B6A88DB7"), Name = "Havre de Grace", ColorId = 4, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 16, RaceWins = 9, RacePlace = 4, RaceShow = 2, Earnings = 2558640, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("FC8B8F89-F6BC-4AC1-8F7F-9F6B7B6CDB42"), Name = "Allez France", ColorId = 1, LegTypeId = LegTypeId.RailRunner, IsMale = false, OwnerId = adminId, RaceStarts = 21, RaceWins = 13, RacePlace = 6, RaceShow = 2, Earnings = 1800000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow },
            new Horse { Id = new Guid("E9B8D831-76B9-4A0D-94F5-C4B6E6A49D34"), Name = "Azeri", ColorId = 2, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 24, RaceWins = 17, RacePlace = 4, RaceShow = 1, Earnings = 4078700, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }
        );

        // Additional Dams
        modelBuilder.Entity<Horse>().HasData(
            new Horse { Id = new Guid("6C3F7C77-7E3D-4A7E-9E92-9C4C6E7D5A11"), Name = "Serena's Song", ColorId = 2, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 38, RaceWins = 18, RacePlace = 11, RaceShow = 3, Earnings = 3283388, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Bay
            new Horse { Id = new Guid("4A2E4E8F-6D3B-4E54-8B4B-5F9A6B2A9E30"), Name = "Royal Delta", ColorId = 3, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 22, RaceWins = 12, RacePlace = 5, RaceShow = 1, Earnings = 4811126, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Seal Brown
            new Horse { Id = new Guid("8E7D1B31-9C68-4E7E-9A34-0B4C2E7D6A55"), Name = "Rags to Riches", ColorId = 4, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 7, RaceWins = 5, RacePlace = 1, RaceShow = 0, Earnings = 1342528, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Chestnut
            new Horse { Id = new Guid("3B9F2C88-5E17-4C61-8A31-7D7C2C4F8A62"), Name = "Zarkava", ColorId = 2, LegTypeId = LegTypeId.LastSpurt, IsMale = false, OwnerId = adminId, RaceStarts = 7, RaceWins = 7, RacePlace = 0, RaceShow = 0, Earnings = 4793000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Bay
            new Horse { Id = new Guid("C5D2F1BA-0C0F-4E1E-9D77-3B1C7E6F3B91"), Name = "Ouija Board", ColorId = 2, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 26, RaceWins = 10, RacePlace = 4, RaceShow = 3, Earnings = 6102000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Bay
            new Horse { Id = new Guid("AF2B6E77-7E1A-44E8-8F9F-6E9B4D1E5A13"), Name = "Makybe Diva", ColorId = 2, LegTypeId = LegTypeId.LastSpurt, IsMale = false, OwnerId = adminId, RaceStarts = 36, RaceWins = 15, RacePlace = 4, RaceShow = 3, Earnings = 14526685, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Bay (AUS$)
            new Horse { Id = new Guid("7D1E2A44-3C9B-4B6F-8B41-1E2C7F6D3A27"), Name = "Sunline", ColorId = 2, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 48, RaceWins = 32, RacePlace = 9, RaceShow = 3, Earnings = 4200000, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Bay
            new Horse { Id = new Guid("F21C9E7B-5A3E-4910-8B3B-5B7A2C1D4E18"), Name = "Genuine Risk", ColorId = 4, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 15, RaceWins = 10, RacePlace = 3, RaceShow = 2, Earnings = 646587, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Chestnut
            new Horse { Id = new Guid("1E0B7A66-2F4E-4D7C-8A9E-3D5B1F7A9C02"), Name = "Winning Colors", ColorId = 1, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 19, RaceWins = 8, RacePlace = 3, RaceShow = 1, Earnings = 1526837, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Gray
            new Horse { Id = new Guid("9C8E4D11-6B7A-4A6B-901E-88E2D3F5A6B4"), Name = "Busher", ColorId = 4, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 21, RaceWins = 15, RacePlace = 3, RaceShow = 1, Earnings = 334035, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Chestnut
            new Horse { Id = new Guid("0B1F3E55-3F7D-49B1-8A5B-B1D2E3C4F5A6"), Name = "Regret", ColorId = 4, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 11, RaceWins = 9, RacePlace = 1, RaceShow = 0, Earnings = 35093, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Chestnut
            new Horse { Id = new Guid("7A9E3D22-4B1C-4D57-92A0-4E6B5C7A8D93"), Name = "Gallorette", ColorId = 4, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 72, RaceWins = 21, RacePlace = 20, RaceShow = 13, Earnings = 445535, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Chestnut
            new Horse { Id = new Guid("D1E7B933-9C0F-4F3C-8B2C-5E7A6D4C3B12"), Name = "Shuvee", ColorId = 4, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 44, RaceWins = 16, RacePlace = 14, RaceShow = 2, Earnings = 890445, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Chestnut
            new Horse { Id = new Guid("E4C1B7A2-6F0D-4C6A-9B9F-4D3E2C1B8A77"), Name = "Bayakoa", ColorId = 2, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 39, RaceWins = 21, RacePlace = 9, RaceShow = 0, Earnings = 2861701, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Bay
            new Horse { Id = new Guid("3F2B1A66-2C7E-48B1-9E33-6A5D4C2B1E99"), Name = "Go for Wand", ColorId = 3, LegTypeId = LegTypeId.FrontRunner, IsMale = false, OwnerId = adminId, RaceStarts = 13, RaceWins = 10, RacePlace = 2, RaceShow = 0, Earnings = 1373338, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Seal Brown
            new Horse { Id = new Guid("B5C1E2A9-7D4F-4327-9F1C-5E7B6A9D2C44"), Name = "Safely Kept", ColorId = 2, LegTypeId = LegTypeId.StartDash, IsMale = false, OwnerId = adminId, RaceStarts = 31, RaceWins = 24, RacePlace = 3, RaceShow = 3, Earnings = 2194206, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Bay
            new Horse { Id = new Guid("F7E3A1D2-5C6B-4B2D-8F1A-3C6E5B7A9D44"), Name = "Ta Wee", ColorId = 2, LegTypeId = LegTypeId.StartDash, IsMale = false, OwnerId = adminId, RaceStarts = 21, RaceWins = 15, RacePlace = 3, RaceShow = 1, Earnings = 284683, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Bay
            new Horse { Id = new Guid("0F9B6E31-4C2D-4D78-9B20-7E1C5A3D9B16"), Name = "Toussaud", ColorId = 3, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 21, RaceWins = 7, RacePlace = 4, RaceShow = 2, Earnings = 551536, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Seal Brown
            new Horse { Id = new Guid("A3D6B5E2-7F9C-4A1E-8B2F-1C6D7E5A9B22"), Name = "Weekend Surprise", ColorId = 4, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 31, RaceWins = 7, RacePlace = 7, RaceShow = 2, Earnings = 402892, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }, // Chestnut
            new Horse { Id = new Guid("2C7B1E6D-5F3A-4D9E-9C1F-7A6B5C2D1E88"), Name = "Hasili", ColorId = 2, LegTypeId = LegTypeId.StretchRunner, IsMale = false, OwnerId = adminId, RaceStarts = 7, RaceWins = 4, RacePlace = 1, RaceShow = 0, Earnings = 68836, IsRetired = true, Parented = 0, CreatedBy = adminId, CreatedDate = DateTimeOffset.UtcNow }
        );

        // Statistics
        modelBuilder.Entity<HorseStatistic>().HasData(
            new HorseStatistic { HorseId = new Guid("649A8C3F-A63D-485F-8809-8404C848BCA0"), StatisticId = StatisticId.Speed, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("649A8C3F-A63D-485F-8809-8404C848BCA0"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("649A8C3F-A63D-485F-8809-8404C848BCA0"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("649A8C3F-A63D-485F-8809-8404C848BCA0"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("AB0E87B3-CDE6-4BD0-9BEF-AAF1C9A5AFB9"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("AB0E87B3-CDE6-4BD0-9BEF-AAF1C9A5AFB9"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("AB0E87B3-CDE6-4BD0-9BEF-AAF1C9A5AFB9"), StatisticId = StatisticId.Agility, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("AB0E87B3-CDE6-4BD0-9BEF-AAF1C9A5AFB9"), StatisticId = StatisticId.Durability, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("F9D3BA4D-26C4-4DAC-87DF-506FF3F8EB87"), StatisticId = StatisticId.Speed, Actual = 60, DominantPotential = 65, RecessivePotential = 55 },
            new HorseStatistic { HorseId = new Guid("F9D3BA4D-26C4-4DAC-87DF-506FF3F8EB87"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("F9D3BA4D-26C4-4DAC-87DF-506FF3F8EB87"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("F9D3BA4D-26C4-4DAC-87DF-506FF3F8EB87"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("39D0F225-0B23-4753-84AE-5394011DF403"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 80, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("39D0F225-0B23-4753-84AE-5394011DF403"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("39D0F225-0B23-4753-84AE-5394011DF403"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 70, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("39D0F225-0B23-4753-84AE-5394011DF403"), StatisticId = StatisticId.Durability, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("05A042EF-0EE5-4660-B2A1-99E6B7D5D294"), StatisticId = StatisticId.Speed, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("05A042EF-0EE5-4660-B2A1-99E6B7D5D294"), StatisticId = StatisticId.Stamina, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("05A042EF-0EE5-4660-B2A1-99E6B7D5D294"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("05A042EF-0EE5-4660-B2A1-99E6B7D5D294"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("AD57472C-1D81-47D7-8618-1D9CE4B0A1B3"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("AD57472C-1D81-47D7-8618-1D9CE4B0A1B3"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("AD57472C-1D81-47D7-8618-1D9CE4B0A1B3"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("AD57472C-1D81-47D7-8618-1D9CE4B0A1B3"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("4FD18871-59D3-4E9E-A519-67D7CCCC70FA"), StatisticId = StatisticId.Speed, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("4FD18871-59D3-4E9E-A519-67D7CCCC70FA"), StatisticId = StatisticId.Stamina, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("4FD18871-59D3-4E9E-A519-67D7CCCC70FA"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("4FD18871-59D3-4E9E-A519-67D7CCCC70FA"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("E907D1D1-FDF8-446C-B11A-DF97C093AAF9"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("E907D1D1-FDF8-446C-B11A-DF97C093AAF9"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("E907D1D1-FDF8-446C-B11A-DF97C093AAF9"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("E907D1D1-FDF8-446C-B11A-DF97C093AAF9"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("2107FD5C-34CF-477F-9DB5-924FD34B22F9"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("2107FD5C-34CF-477F-9DB5-924FD34B22F9"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 75, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("2107FD5C-34CF-477F-9DB5-924FD34B22F9"), StatisticId = StatisticId.Agility, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("2107FD5C-34CF-477F-9DB5-924FD34B22F9"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("210BB356-F8AE-4DBE-98F6-16DFA20CF930"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("210BB356-F8AE-4DBE-98F6-16DFA20CF930"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 70, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("210BB356-F8AE-4DBE-98F6-16DFA20CF930"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("210BB356-F8AE-4DBE-98F6-16DFA20CF930"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("D1F1A1C6-885B-4D27-9375-572C7D42EF32"), StatisticId = StatisticId.Speed, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("D1F1A1C6-885B-4D27-9375-572C7D42EF32"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("D1F1A1C6-885B-4D27-9375-572C7D42EF32"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("D1F1A1C6-885B-4D27-9375-572C7D42EF32"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("B10F1F36-39F2-4F91-AE43-B727ED1D9FF8"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("B10F1F36-39F2-4F91-AE43-B727ED1D9FF8"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("B10F1F36-39F2-4F91-AE43-B727ED1D9FF8"), StatisticId = StatisticId.Agility, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("B10F1F36-39F2-4F91-AE43-B727ED1D9FF8"), StatisticId = StatisticId.Durability, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("3E7FE0A3-DA6E-4F08-B12E-48D8D7B1F6DA"), StatisticId = StatisticId.Speed, Actual = 60, DominantPotential = 65, RecessivePotential = 55 },
            new HorseStatistic { HorseId = new Guid("3E7FE0A3-DA6E-4F08-B12E-48D8D7B1F6DA"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("3E7FE0A3-DA6E-4F08-B12E-48D8D7B1F6DA"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("3E7FE0A3-DA6E-4F08-B12E-48D8D7B1F6DA"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("A1D5E6F4-3D6B-46BC-A89F-072BF74A6BA8"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 80, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("A1D5E6F4-3D6B-46BC-A89F-072BF74A6BA8"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("A1D5E6F4-3D6B-46BC-A89F-072BF74A6BA8"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 70, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("A1D5E6F4-3D6B-46BC-A89F-072BF74A6BA8"), StatisticId = StatisticId.Durability, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("7F6B85E9-82A5-4C7C-89D6-BE1F0AB4C8E4"), StatisticId = StatisticId.Speed, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("7F6B85E9-82A5-4C7C-89D6-BE1F0AB4C8E4"), StatisticId = StatisticId.Stamina, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("7F6B85E9-82A5-4C7C-89D6-BE1F0AB4C8E4"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("7F6B85E9-82A5-4C7C-89D6-BE1F0AB4C8E4"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("B16FB2C8-519E-4C72-9E39-F586B42A4C6B"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("B16FB2C8-519E-4C72-9E39-F586B42A4C6B"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("B16FB2C8-519E-4C72-9E39-F586B42A4C6B"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("B16FB2C8-519E-4C72-9E39-F586B42A4C6B"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("B2F8BA89-073B-48F3-9C9E-1EFBED6D093E"), StatisticId = StatisticId.Speed, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("B2F8BA89-073B-48F3-9C9E-1EFBED6D093E"), StatisticId = StatisticId.Stamina, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("B2F8BA89-073B-48F3-9C9E-1EFBED6D093E"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("B2F8BA89-073B-48F3-9C9E-1EFBED6D093E"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("47ED5B3D-0ED4-4A1B-BF1B-2D2B09FF5E5C"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("47ED5B3D-0ED4-4A1B-BF1B-2D2B09FF5E5C"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("47ED5B3D-0ED4-4A1B-BF1B-2D2B09FF5E5C"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("47ED5B3D-0ED4-4A1B-BF1B-2D2B09FF5E5C"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("EB601D48-CB8E-4B4F-8DFE-09F9F5F6B0C4"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("EB601D48-CB8E-4B4F-8DFE-09F9F5F6B0C4"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 75, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("EB601D48-CB8E-4B4F-8DFE-09F9F5F6B0C4"), StatisticId = StatisticId.Agility, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("EB601D48-CB8E-4B4F-8DFE-09F9F5F6B0C4"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("6B7A5F42-1D3E-4E0F-AC1C-59D8E4E9E2E7"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("6B7A5F42-1D3E-4E0F-AC1C-59D8E4E9E2E7"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 70, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("6B7A5F42-1D3E-4E0F-AC1C-59D8E4E9E2E7"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("6B7A5F42-1D3E-4E0F-AC1C-59D8E4E9E2E7"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("C1A38B67-5A4C-4E87-9D18-3A4DA6E6A6F7"), StatisticId = StatisticId.Speed, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("C1A38B67-5A4C-4E87-9D18-3A4DA6E6A6F7"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("C1A38B67-5A4C-4E87-9D18-3A4DA6E6A6F7"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("C1A38B67-5A4C-4E87-9D18-3A4DA6E6A6F7"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("B8F9D7B2-9A35-4B4F-8F7C-56F8E2E9E8E3"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("B8F9D7B2-9A35-4B4F-8F7C-56F8E2E9E8E3"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("B8F9D7B2-9A35-4B4F-8F7C-56F8E2E9E8E3"), StatisticId = StatisticId.Agility, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("B8F9D7B2-9A35-4B4F-8F7C-56F8E2E9E8E3"), StatisticId = StatisticId.Durability, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("A3F2C1B8-9B57-4D3A-8E2C-1F7D8E4E9E2B"), StatisticId = StatisticId.Speed, Actual = 60, DominantPotential = 65, RecessivePotential = 55 },
            new HorseStatistic { HorseId = new Guid("A3F2C1B8-9B57-4D3A-8E2C-1F7D8E4E9E2B"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("A3F2C1B8-9B57-4D3A-8E2C-1F7D8E4E9E2B"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("A3F2C1B8-9B57-4D3A-8E2C-1F7D8E4E9E2B"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("5A6F7E3D-1D3A-4B6F-9C9E-8E7E6E6E2E7D"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 80, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("5A6F7E3D-1D3A-4B6F-9C9E-8E7E6E6E2E7D"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("5A6F7E3D-1D3A-4B6F-9C9E-8E7E6E6E2E7D"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 70, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("5A6F7E3D-1D3A-4B6F-9C9E-8E7E6E6E2E7D"), StatisticId = StatisticId.Durability, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("A18A61FA-18E7-4C2B-8A8B-1D9F2F3E3DB1"), StatisticId = StatisticId.Speed, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("A18A61FA-18E7-4C2B-8A8B-1D9F2F3E3DB1"), StatisticId = StatisticId.Stamina, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("A18A61FA-18E7-4C2B-8A8B-1D9F2F3E3DB1"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("A18A61FA-18E7-4C2B-8A8B-1D9F2F3E3DB1"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("639B2D9E-33E3-41DA-9B9F-936A97E59F77"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("639B2D9E-33E3-41DA-9B9F-936A97E59F77"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("639B2D9E-33E3-41DA-9B9F-936A97E59F77"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("639B2D9E-33E3-41DA-9B9F-936A97E59F77"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("60A9B06A-B1A6-4844-AE8C-B52E2D785540"), StatisticId = StatisticId.Speed, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("60A9B06A-B1A6-4844-AE8C-B52E2D785540"), StatisticId = StatisticId.Stamina, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("60A9B06A-B1A6-4844-AE8C-B52E2D785540"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("60A9B06A-B1A6-4844-AE8C-B52E2D785540"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("1A737DBC-89CE-4C6C-A833-9F76B2D75998"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("1A737DBC-89CE-4C6C-A833-9F76B2D75998"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("1A737DBC-89CE-4C6C-A833-9F76B2D75998"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("1A737DBC-89CE-4C6C-A833-9F76B2D75998"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("FB9D4345-6D6E-4C5E-9AC7-C8E1A3C5A3FA"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("FB9D4345-6D6E-4C5E-9AC7-C8E1A3C5A3FA"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 75, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("FB9D4345-6D6E-4C5E-9AC7-C8E1A3C5A3FA"), StatisticId = StatisticId.Agility, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("FB9D4345-6D6E-4C5E-9AC7-C8E1A3C5A3FA"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("8D52A8A3-1339-4B49-AE8A-AC2CFA723B5C"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("8D52A8A3-1339-4B49-AE8A-AC2CFA723B5C"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 70, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("8D52A8A3-1339-4B49-AE8A-AC2CFA723B5C"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("8D52A8A3-1339-4B49-AE8A-AC2CFA723B5C"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("3917B927-E776-4DDB-9193-9F19E0DC84D0"), StatisticId = StatisticId.Speed, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("3917B927-E776-4DDB-9193-9F19E0DC84D0"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("3917B927-E776-4DDB-9193-9F19E0DC84D0"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("3917B927-E776-4DDB-9193-9F19E0DC84D0"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("BFC7B007-6D6C-4723-933B-9FC58D4A1B50"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("BFC7B007-6D6C-4723-933B-9FC58D4A1B50"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("BFC7B007-6D6C-4723-933B-9FC58D4A1B50"), StatisticId = StatisticId.Agility, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("BFC7B007-6D6C-4723-933B-9FC58D4A1B50"), StatisticId = StatisticId.Durability, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("D4695321-5F97-4D37-BE34-8E6D50E47E4D"), StatisticId = StatisticId.Speed, Actual = 60, DominantPotential = 65, RecessivePotential = 55 },
            new HorseStatistic { HorseId = new Guid("D4695321-5F97-4D37-BE34-8E6D50E47E4D"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("D4695321-5F97-4D37-BE34-8E6D50E47E4D"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("D4695321-5F97-4D37-BE34-8E6D50E47E4D"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("947C95DE-C0C8-468C-B6DF-6A54228A16F1"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 80, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("947C95DE-C0C8-468C-B6DF-6A54228A16F1"), StatisticId = StatisticId.Stamina, Actual = 65, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("947C95DE-C0C8-468C-B6DF-6A54228A16F1"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 70, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("947C95DE-C0C8-468C-B6DF-6A54228A16F1"), StatisticId = StatisticId.Durability, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("F0EC7B9A-5567-4E5C-8D48-8A6E354B8C8A"), StatisticId = StatisticId.Speed, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("F0EC7B9A-5567-4E5C-8D48-8A6E354B8C8A"), StatisticId = StatisticId.Stamina, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("F0EC7B9A-5567-4E5C-8D48-8A6E354B8C8A"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("F0EC7B9A-5567-4E5C-8D48-8A6E354B8C8A"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("D6A71C90-1298-4938-8F8F-128FC4C8F10D"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("D6A71C90-1298-4938-8F8F-128FC4C8F10D"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("D6A71C90-1298-4938-8F8F-128FC4C8F10D"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("D6A71C90-1298-4938-8F8F-128FC4C8F10D"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 60 },
            new HorseStatistic { HorseId = new Guid("D8E2AA39-5D84-4D37-AF45-92497BBE0C08"), StatisticId = StatisticId.Speed, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("D8E2AA39-5D84-4D37-AF45-92497BBE0C08"), StatisticId = StatisticId.Stamina, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("D8E2AA39-5D84-4D37-AF45-92497BBE0C08"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("D8E2AA39-5D84-4D37-AF45-92497BBE0C08"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("9ACFAE7F-60F1-420C-813B-C3E3B6A88DB7"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("9ACFAE7F-60F1-420C-813B-C3E3B6A88DB7"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("9ACFAE7F-60F1-420C-813B-C3E3B6A88DB7"), StatisticId = StatisticId.Agility, Actual = 60, DominantPotential = 65, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("9ACFAE7F-60F1-420C-813B-C3E3B6A88DB7"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("FC8B8F89-F6BC-4AC1-8F7F-9F6B7B6CDB42"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 75, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("FC8B8F89-F6BC-4AC1-8F7F-9F6B7B6CDB42"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 75, RecessivePotential = 85 },
            new HorseStatistic { HorseId = new Guid("FC8B8F89-F6BC-4AC1-8F7F-9F6B7B6CDB42"), StatisticId = StatisticId.Agility, Actual = 65, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("FC8B8F89-F6BC-4AC1-8F7F-9F6B7B6CDB42"), StatisticId = StatisticId.Durability, Actual = 65, DominantPotential = 70, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("E9B8D831-76B9-4A0D-94F5-C4B6E6A49D34"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 70, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("E9B8D831-76B9-4A0D-94F5-C4B6E6A49D34"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 70, RecessivePotential = 80 },
            new HorseStatistic { HorseId = new Guid("E9B8D831-76B9-4A0D-94F5-C4B6E6A49D34"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 75, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("E9B8D831-76B9-4A0D-94F5-C4B6E6A49D34"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 75, RecessivePotential = 70 }
        );

        // Statistics for NEW SIRES
        modelBuilder.Entity<HorseStatistic>().HasData(
            // Bold Ruler
            new HorseStatistic { HorseId = new Guid("b8a4f3c5-6d9c-4d8c-9f0b-7c9d1b3a5f21"), StatisticId = StatisticId.Speed, Actual = 78, DominantPotential = 82, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("b8a4f3c5-6d9c-4d8c-9f0b-7c9d1b3a5f21"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 75, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("b8a4f3c5-6d9c-4d8c-9f0b-7c9d1b3a5f21"), StatisticId = StatisticId.Agility, Actual = 72, DominantPotential = 78, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("b8a4f3c5-6d9c-4d8c-9f0b-7c9d1b3a5f21"), StatisticId = StatisticId.Durability, Actual = 66, DominantPotential = 71, RecessivePotential = 64 },

            // Buckpasser
            new HorseStatistic { HorseId = new Guid("b1b3c9d2-3e80-4f0d-9a78-1a2f33e7c4a9"), StatisticId = StatisticId.Speed, Actual = 74, DominantPotential = 80, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("b1b3c9d2-3e80-4f0d-9a78-1a2f33e7c4a9"), StatisticId = StatisticId.Stamina, Actual = 77, DominantPotential = 82, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("b1b3c9d2-3e80-4f0d-9a78-1a2f33e7c4a9"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 76, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("b1b3c9d2-3e80-4f0d-9a78-1a2f33e7c4a9"), StatisticId = StatisticId.Durability, Actual = 73, DominantPotential = 79, RecessivePotential = 70 },

            // Mr. Prospector
            new HorseStatistic { HorseId = new Guid("4c5f2e8a-1f0b-4bde-9a40-3b1c9a6d7e22"), StatisticId = StatisticId.Speed, Actual = 76, DominantPotential = 81, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("4c5f2e8a-1f0b-4bde-9a40-3b1c9a6d7e22"), StatisticId = StatisticId.Stamina, Actual = 68, DominantPotential = 72, RecessivePotential = 66 },
            new HorseStatistic { HorseId = new Guid("4c5f2e8a-1f0b-4bde-9a40-3b1c9a6d7e22"), StatisticId = StatisticId.Agility, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("4c5f2e8a-1f0b-4bde-9a40-3b1c9a6d7e22"), StatisticId = StatisticId.Durability, Actual = 67, DominantPotential = 71, RecessivePotential = 65 },

            // Storm Cat
            new HorseStatistic { HorseId = new Guid("0e7a2c91-7c5a-4a54-9b1f-2f7d9c3e1a88"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 81, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("0e7a2c91-7c5a-4a54-9b1f-2f7d9c3e1a88"), StatisticId = StatisticId.Stamina, Actual = 69, DominantPotential = 73, RecessivePotential = 66 },
            new HorseStatistic { HorseId = new Guid("0e7a2c91-7c5a-4a54-9b1f-2f7d9c3e1a88"), StatisticId = StatisticId.Agility, Actual = 76, DominantPotential = 80, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("0e7a2c91-7c5a-4a54-9b1f-2f7d9c3e1a88"), StatisticId = StatisticId.Durability, Actual = 66, DominantPotential = 70, RecessivePotential = 64 },

            // A.P. Indy
            new HorseStatistic { HorseId = new Guid("9f2d6a1b-8c34-4d77-9a1c-5e0f4c2b7d11"), StatisticId = StatisticId.Speed, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("9f2d6a1b-8c34-4d77-9a1c-5e0f4c2b7d11"), StatisticId = StatisticId.Stamina, Actual = 78, DominantPotential = 83, RecessivePotential = 76 },
            new HorseStatistic { HorseId = new Guid("9f2d6a1b-8c34-4d77-9a1c-5e0f4c2b7d11"), StatisticId = StatisticId.Agility, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("9f2d6a1b-8c34-4d77-9a1c-5e0f4c2b7d11"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },

            // Danzig
            new HorseStatistic { HorseId = new Guid("a6d4c3f1-2b7e-4a9c-8e5d-1f2c3a4b5e66"), StatisticId = StatisticId.Speed, Actual = 77, DominantPotential = 82, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("a6d4c3f1-2b7e-4a9c-8e5d-1f2c3a4b5e66"), StatisticId = StatisticId.Stamina, Actual = 67, DominantPotential = 72, RecessivePotential = 65 },
            new HorseStatistic { HorseId = new Guid("a6d4c3f1-2b7e-4a9c-8e5d-1f2c3a4b5e66"), StatisticId = StatisticId.Agility, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("a6d4c3f1-2b7e-4a9c-8e5d-1f2c3a4b5e66"), StatisticId = StatisticId.Durability, Actual = 66, DominantPotential = 70, RecessivePotential = 64 },

            // Fappiano
            new HorseStatistic { HorseId = new Guid("c4e1b2a7-97d0-4a2a-9a6c-7d1e2f3c4b55"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("c4e1b2a7-97d0-4a2a-9a6c-7d1e2f3c4b55"), StatisticId = StatisticId.Stamina, Actual = 76, DominantPotential = 81, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("c4e1b2a7-97d0-4a2a-9a6c-7d1e2f3c4b55"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("c4e1b2a7-97d0-4a2a-9a6c-7d1e2f3c4b55"), StatisticId = StatisticId.Durability, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },

            // Unbridled
            new HorseStatistic { HorseId = new Guid("7b3a9c1e-5f84-4cfd-9ad2-3e1b7f0c2a40"), StatisticId = StatisticId.Speed, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("7b3a9c1e-5f84-4cfd-9ad2-3e1b7f0c2a40"), StatisticId = StatisticId.Stamina, Actual = 77, DominantPotential = 82, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("7b3a9c1e-5f84-4cfd-9ad2-3e1b7f0c2a40"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("7b3a9c1e-5f84-4cfd-9ad2-3e1b7f0c2a40"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },

            // Distorted Humor
            new HorseStatistic { HorseId = new Guid("5e2a7d9c-3b1f-4f0e-8a6d-1c2b3a4e5f77"), StatisticId = StatisticId.Speed, Actual = 74, DominantPotential = 79, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("5e2a7d9c-3b1f-4f0e-8a6d-1c2b3a4e5f77"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("5e2a7d9c-3b1f-4f0e-8a6d-1c2b3a4e5f77"), StatisticId = StatisticId.Agility, Actual = 73, DominantPotential = 78, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("5e2a7d9c-3b1f-4f0e-8a6d-1c2b3a4e5f77"), StatisticId = StatisticId.Durability, Actual = 69, DominantPotential = 73, RecessivePotential = 67 },

            // Smart Strike
            new HorseStatistic { HorseId = new Guid("2f6b1c7a-8e9d-4a3c-9b1e-5a7d2c4f0e11"), StatisticId = StatisticId.Speed, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("2f6b1c7a-8e9d-4a3c-9b1e-5a7d2c4f0e11"), StatisticId = StatisticId.Stamina, Actual = 74, DominantPotential = 79, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("2f6b1c7a-8e9d-4a3c-9b1e-5a7d2c4f0e11"), StatisticId = StatisticId.Agility, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("2f6b1c7a-8e9d-4a3c-9b1e-5a7d2c4f0e11"), StatisticId = StatisticId.Durability, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },

            // Medaglia d'Oro
            new HorseStatistic { HorseId = new Guid("d7e4a2b1-6c3f-4a9e-8b2d-1f0e7c5a3b66"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("d7e4a2b1-6c3f-4a9e-8b2d-1f0e7c5a3b66"), StatisticId = StatisticId.Stamina, Actual = 76, DominantPotential = 81, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("d7e4a2b1-6c3f-4a9e-8b2d-1f0e7c5a3b66"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("d7e4a2b1-6c3f-4a9e-8b2d-1f0e7c5a3b66"), StatisticId = StatisticId.Durability, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },

            // Bernardini
            new HorseStatistic { HorseId = new Guid("1a9c3e7f-2b5d-47a6-9c1e-8d0f2a3b4c55"), StatisticId = StatisticId.Speed, Actual = 73, DominantPotential = 79, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("1a9c3e7f-2b5d-47a6-9c1e-8d0f2a3b4c55"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("1a9c3e7f-2b5d-47a6-9c1e-8d0f2a3b4c55"), StatisticId = StatisticId.Agility, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("1a9c3e7f-2b5d-47a6-9c1e-8d0f2a3b4c55"), StatisticId = StatisticId.Durability, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },

            // Street Cry
            new HorseStatistic { HorseId = new Guid("6c2b7e1a-9f0d-4a3b-8c5e-1d2f3a4b5c88"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("6c2b7e1a-9f0d-4a3b-8c5e-1d2f3a4b5c88"), StatisticId = StatisticId.Stamina, Actual = 77, DominantPotential = 82, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("6c2b7e1a-9f0d-4a3b-8c5e-1d2f3a4b5c88"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("6c2b7e1a-9f0d-4a3b-8c5e-1d2f3a4b5c88"), StatisticId = StatisticId.Durability, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },

            // Empire Maker
            new HorseStatistic { HorseId = new Guid("f0a3b2c1-6e7d-4d9a-8c1b-2a5f3e7c4b90"), StatisticId = StatisticId.Speed, Actual = 71, DominantPotential = 76, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("f0a3b2c1-6e7d-4d9a-8c1b-2a5f3e7c4b90"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("f0a3b2c1-6e7d-4d9a-8c1b-2a5f3e7c4b90"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("f0a3b2c1-6e7d-4d9a-8c1b-2a5f3e7c4b90"), StatisticId = StatisticId.Durability, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },

            // Mineshaft
            new HorseStatistic { HorseId = new Guid("3b7e1c5a-2f0d-4a9c-8e3b-1d6f2a4c5e22"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("3b7e1c5a-2f0d-4a9c-8e3b-1d6f2a4c5e22"), StatisticId = StatisticId.Stamina, Actual = 78, DominantPotential = 83, RecessivePotential = 76 },
            new HorseStatistic { HorseId = new Guid("3b7e1c5a-2f0d-4a9c-8e3b-1d6f2a4c5e22"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("3b7e1c5a-2f0d-4a9c-8e3b-1d6f2a4c5e22"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },

            // Pioneerof the Nile
            new HorseStatistic { HorseId = new Guid("e6a1d2f3-7c4b-4e9a-9b0d-2f5a3c7e1b44"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("e6a1d2f3-7c4b-4e9a-9b0d-2f5a3c7e1b44"), StatisticId = StatisticId.Stamina, Actual = 74, DominantPotential = 79, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("e6a1d2f3-7c4b-4e9a-9b0d-2f5a3c7e1b44"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("e6a1d2f3-7c4b-4e9a-9b0d-2f5a3c7e1b44"), StatisticId = StatisticId.Durability, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },

            // Quality Road
            new HorseStatistic { HorseId = new Guid("9c2f1e7a-3b5d-4a8c-9d0e-1f6a2c4b5e77"), StatisticId = StatisticId.Speed, Actual = 76, DominantPotential = 81, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("9c2f1e7a-3b5d-4a8c-9d0e-1f6a2c4b5e77"), StatisticId = StatisticId.Stamina, Actual = 72, DominantPotential = 76, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("9c2f1e7a-3b5d-4a8c-9d0e-1f6a2c4b5e77"), StatisticId = StatisticId.Agility, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("9c2f1e7a-3b5d-4a8c-9d0e-1f6a2c4b5e77"), StatisticId = StatisticId.Durability, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },

            // Speightstown
            new HorseStatistic { HorseId = new Guid("0b7e3c2a-5f1d-4a9c-8e6b-2d3f1a4c5e19"), StatisticId = StatisticId.Speed, Actual = 77, DominantPotential = 82, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("0b7e3c2a-5f1d-4a9c-8e6b-2d3f1a4c5e19"), StatisticId = StatisticId.Stamina, Actual = 69, DominantPotential = 73, RecessivePotential = 67 },
            new HorseStatistic { HorseId = new Guid("0b7e3c2a-5f1d-4a9c-8e6b-2d3f1a4c5e19"), StatisticId = StatisticId.Agility, Actual = 75, DominantPotential = 80, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("0b7e3c2a-5f1d-4a9c-8e6b-2d3f1a4c5e19"), StatisticId = StatisticId.Durability, Actual = 68, DominantPotential = 72, RecessivePotential = 66 },

            // Tapit
            new HorseStatistic { HorseId = new Guid("a2e7b5c1-4d9f-4fa0-9c1e-7b3a6d2f0c66"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 76, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("a2e7b5c1-4d9f-4fa0-9c1e-7b3a6d2f0c66"), StatisticId = StatisticId.Stamina, Actual = 74, DominantPotential = 79, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("a2e7b5c1-4d9f-4fa0-9c1e-7b3a6d2f0c66"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("a2e7b5c1-4d9f-4fa0-9c1e-7b3a6d2f0c66"), StatisticId = StatisticId.Durability, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },

            // Curlin
            new HorseStatistic { HorseId = new Guid("7e1c3b5a-2f9d-4a0c-8d6e-1a2f4c5b7e33"), StatisticId = StatisticId.Speed, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("7e1c3b5a-2f9d-4a0c-8d6e-1a2f4c5b7e33"), StatisticId = StatisticId.Stamina, Actual = 79, DominantPotential = 84, RecessivePotential = 77 },
            new HorseStatistic { HorseId = new Guid("7e1c3b5a-2f9d-4a0c-8d6e-1a2f4c5b7e33"), StatisticId = StatisticId.Agility, Actual = 72, DominantPotential = 76, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("7e1c3b5a-2f9d-4a0c-8d6e-1a2f4c5b7e33"), StatisticId = StatisticId.Durability, Actual = 76, DominantPotential = 81, RecessivePotential = 74 }
        );

        // Statistics for NEW DAMS
        modelBuilder.Entity<HorseStatistic>().HasData(
            // Serena's Song
            new HorseStatistic { HorseId = new Guid("6C3F7C77-7E3D-4A7E-9E92-9C4C6E7D5A11"), StatisticId = StatisticId.Speed, Actual = 74, DominantPotential = 79, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("6C3F7C77-7E3D-4A7E-9E92-9C4C6E7D5A11"), StatisticId = StatisticId.Stamina, Actual = 73, DominantPotential = 78, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("6C3F7C77-7E3D-4A7E-9E92-9C4C6E7D5A11"), StatisticId = StatisticId.Agility, Actual = 72, DominantPotential = 76, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("6C3F7C77-7E3D-4A7E-9E92-9C4C6E7D5A11"), StatisticId = StatisticId.Durability, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },

            // Royal Delta
            new HorseStatistic { HorseId = new Guid("4A2E4E8F-6D3B-4E54-8B4B-5F9A6B2A9E30"), StatisticId = StatisticId.Speed, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("4A2E4E8F-6D3B-4E54-8B4B-5F9A6B2A9E30"), StatisticId = StatisticId.Stamina, Actual = 76, DominantPotential = 81, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("4A2E4E8F-6D3B-4E54-8B4B-5F9A6B2A9E30"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("4A2E4E8F-6D3B-4E54-8B4B-5F9A6B2A9E30"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },

            // Rags to Riches
            new HorseStatistic { HorseId = new Guid("8E7D1B31-9C68-4E7E-9A34-0B4C2E7D6A55"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("8E7D1B31-9C68-4E7E-9A34-0B4C2E7D6A55"), StatisticId = StatisticId.Stamina, Actual = 77, DominantPotential = 82, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("8E7D1B31-9C68-4E7E-9A34-0B4C2E7D6A55"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("8E7D1B31-9C68-4E7E-9A34-0B4C2E7D6A55"), StatisticId = StatisticId.Durability, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },

            // Zarkava
            new HorseStatistic { HorseId = new Guid("3B9F2C88-5E17-4C61-8A31-7D7C2C4F8A62"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("3B9F2C88-5E17-4C61-8A31-7D7C2C4F8A62"), StatisticId = StatisticId.Stamina, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("3B9F2C88-5E17-4C61-8A31-7D7C2C4F8A62"), StatisticId = StatisticId.Agility, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("3B9F2C88-5E17-4C61-8A31-7D7C2C4F8A62"), StatisticId = StatisticId.Durability, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },

            // Ouija Board
            new HorseStatistic { HorseId = new Guid("C5D2F1BA-0C0F-4E1E-9D77-3B1C7E6F3B91"), StatisticId = StatisticId.Speed, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("C5D2F1BA-0C0F-4E1E-9D77-3B1C7E6F3B91"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("C5D2F1BA-0C0F-4E1E-9D77-3B1C7E6F3B91"), StatisticId = StatisticId.Agility, Actual = 72, DominantPotential = 76, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("C5D2F1BA-0C0F-4E1E-9D77-3B1C7E6F3B91"), StatisticId = StatisticId.Durability, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },

            // Makybe Diva
            new HorseStatistic { HorseId = new Guid("AF2B6E77-7E1A-44E8-8F9F-6E9B4D1E5A13"), StatisticId = StatisticId.Speed, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("AF2B6E77-7E1A-44E8-8F9F-6E9B4D1E5A13"), StatisticId = StatisticId.Stamina, Actual = 80, DominantPotential = 85, RecessivePotential = 78 },
            new HorseStatistic { HorseId = new Guid("AF2B6E77-7E1A-44E8-8F9F-6E9B4D1E5A13"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("AF2B6E77-7E1A-44E8-8F9F-6E9B4D1E5A13"), StatisticId = StatisticId.Durability, Actual = 78, DominantPotential = 83, RecessivePotential = 76 },

            // Sunline
            new HorseStatistic { HorseId = new Guid("7D1E2A44-3C9B-4B6F-8B41-1E2C7F6D3A27"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("7D1E2A44-3C9B-4B6F-8B41-1E2C7F6D3A27"), StatisticId = StatisticId.Stamina, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("7D1E2A44-3C9B-4B6F-8B41-1E2C7F6D3A27"), StatisticId = StatisticId.Agility, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("7D1E2A44-3C9B-4B6F-8B41-1E2C7F6D3A27"), StatisticId = StatisticId.Durability, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },

            // Genuine Risk
            new HorseStatistic { HorseId = new Guid("F21C9E7B-5A3E-4910-8B3B-5B7A2C1D4E18"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("F21C9E7B-5A3E-4910-8B3B-5B7A2C1D4E18"), StatisticId = StatisticId.Stamina, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("F21C9E7B-5A3E-4910-8B3B-5B7A2C1D4E18"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("F21C9E7B-5A3E-4910-8B3B-5B7A2C1D4E18"), StatisticId = StatisticId.Durability, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },

            // Winning Colors
            new HorseStatistic { HorseId = new Guid("1E0B7A66-2F4E-4D7C-8A9E-3D5B1F7A9C02"), StatisticId = StatisticId.Speed, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("1E0B7A66-2F4E-4D7C-8A9E-3D5B1F7A9C02"), StatisticId = StatisticId.Stamina, Actual = 72, DominantPotential = 76, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("1E0B7A66-2F4E-4D7C-8A9E-3D5B1F7A9C02"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("1E0B7A66-2F4E-4D7C-8A9E-3D5B1F7A9C02"), StatisticId = StatisticId.Durability, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },

            // Busher
            new HorseStatistic { HorseId = new Guid("9C8E4D11-6B7A-4A6B-901E-88E2D3F5A6B4"), StatisticId = StatisticId.Speed, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("9C8E4D11-6B7A-4A6B-901E-88E2D3F5A6B4"), StatisticId = StatisticId.Stamina, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("9C8E4D11-6B7A-4A6B-901E-88E2D3F5A6B4"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("9C8E4D11-6B7A-4A6B-901E-88E2D3F5A6B4"), StatisticId = StatisticId.Durability, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },

            // Regret
            new HorseStatistic { HorseId = new Guid("0B1F3E55-3F7D-49B1-8A5B-B1D2E3C4F5A6"), StatisticId = StatisticId.Speed, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("0B1F3E55-3F7D-49B1-8A5B-B1D2E3C4F5A6"), StatisticId = StatisticId.Stamina, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("0B1F3E55-3F7D-49B1-8A5B-B1D2E3C4F5A6"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("0B1F3E55-3F7D-49B1-8A5B-B1D2E3C4F5A6"), StatisticId = StatisticId.Durability, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },

            // Gallorette
            new HorseStatistic { HorseId = new Guid("7A9E3D22-4B1C-4D57-92A0-4E6B5C7A8D93"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("7A9E3D22-4B1C-4D57-92A0-4E6B5C7A8D93"), StatisticId = StatisticId.Stamina, Actual = 76, DominantPotential = 81, RecessivePotential = 74 },
            new HorseStatistic { HorseId = new Guid("7A9E3D22-4B1C-4D57-92A0-4E6B5C7A8D93"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("7A9E3D22-4B1C-4D57-92A0-4E6B5C7A8D93"), StatisticId = StatisticId.Durability, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },

            // Shuvee
            new HorseStatistic { HorseId = new Guid("D1E7B933-9C0F-4F3C-8B2C-5E7A6D4C3B12"), StatisticId = StatisticId.Speed, Actual = 72, DominantPotential = 76, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("D1E7B933-9C0F-4F3C-8B2C-5E7A6D4C3B12"), StatisticId = StatisticId.Stamina, Actual = 77, DominantPotential = 82, RecessivePotential = 75 },
            new HorseStatistic { HorseId = new Guid("D1E7B933-9C0F-4F3C-8B2C-5E7A6D4C3B12"), StatisticId = StatisticId.Agility, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("D1E7B933-9C0F-4F3C-8B2C-5E7A6D4C3B12"), StatisticId = StatisticId.Durability, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },

            // Bayakoa
            new HorseStatistic { HorseId = new Guid("E4C1B7A2-6F0D-4C6A-9B9F-4D3E2C1B8A77"), StatisticId = StatisticId.Speed, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("E4C1B7A2-6F0D-4C6A-9B9F-4D3E2C1B8A77"), StatisticId = StatisticId.Stamina, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("E4C1B7A2-6F0D-4C6A-9B9F-4D3E2C1B8A77"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("E4C1B7A2-6F0D-4C6A-9B9F-4D3E2C1B8A77"), StatisticId = StatisticId.Durability, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },

            // Go for Wand
            new HorseStatistic { HorseId = new Guid("3F2B1A66-2C7E-48B1-9E33-6A5D4C2B1E99"), StatisticId = StatisticId.Speed, Actual = 75, DominantPotential = 80, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("3F2B1A66-2C7E-48B1-9E33-6A5D4C2B1E99"), StatisticId = StatisticId.Stamina, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("3F2B1A66-2C7E-48B1-9E33-6A5D4C2B1E99"), StatisticId = StatisticId.Agility, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("3F2B1A66-2C7E-48B1-9E33-6A5D4C2B1E99"), StatisticId = StatisticId.Durability, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },

            // Safely Kept
            new HorseStatistic { HorseId = new Guid("B5C1E2A9-7D4F-4327-9F1C-5E7B6A9D2C44"), StatisticId = StatisticId.Speed, Actual = 76, DominantPotential = 81, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("B5C1E2A9-7D4F-4327-9F1C-5E7B6A9D2C44"), StatisticId = StatisticId.Stamina, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("B5C1E2A9-7D4F-4327-9F1C-5E7B6A9D2C44"), StatisticId = StatisticId.Agility, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("B5C1E2A9-7D4F-4327-9F1C-5E7B6A9D2C44"), StatisticId = StatisticId.Durability, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },

            // Ta Wee
            new HorseStatistic { HorseId = new Guid("F7E3A1D2-5C6B-4B2D-8F1A-3C6E5B7A9D44"), StatisticId = StatisticId.Speed, Actual = 76, DominantPotential = 81, RecessivePotential = 73 },
            new HorseStatistic { HorseId = new Guid("F7E3A1D2-5C6B-4B2D-8F1A-3C6E5B7A9D44"), StatisticId = StatisticId.Stamina, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("F7E3A1D2-5C6B-4B2D-8F1A-3C6E5B7A9D44"), StatisticId = StatisticId.Agility, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("F7E3A1D2-5C6B-4B2D-8F1A-3C6E5B7A9D44"), StatisticId = StatisticId.Durability, Actual = 69, DominantPotential = 73, RecessivePotential = 67 },

            // Toussaud
            new HorseStatistic { HorseId = new Guid("0F9B6E31-4C2D-4D78-9B20-7E1C5A3D9B16"), StatisticId = StatisticId.Speed, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("0F9B6E31-4C2D-4D78-9B20-7E1C5A3D9B16"), StatisticId = StatisticId.Stamina, Actual = 73, DominantPotential = 78, RecessivePotential = 71 },
            new HorseStatistic { HorseId = new Guid("0F9B6E31-4C2D-4D78-9B20-7E1C5A3D9B16"), StatisticId = StatisticId.Agility, Actual = 72, DominantPotential = 76, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("0F9B6E31-4C2D-4D78-9B20-7E1C5A3D9B16"), StatisticId = StatisticId.Durability, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },

            // Weekend Surprise
            new HorseStatistic { HorseId = new Guid("A3D6B5E2-7F9C-4A1E-8B2F-1C6D7E5A9B22"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("A3D6B5E2-7F9C-4A1E-8B2F-1C6D7E5A9B22"), StatisticId = StatisticId.Stamina, Actual = 74, DominantPotential = 79, RecessivePotential = 72 },
            new HorseStatistic { HorseId = new Guid("A3D6B5E2-7F9C-4A1E-8B2F-1C6D7E5A9B22"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("A3D6B5E2-7F9C-4A1E-8B2F-1C6D7E5A9B22"), StatisticId = StatisticId.Durability, Actual = 72, DominantPotential = 77, RecessivePotential = 70 },

            // Hasili
            new HorseStatistic { HorseId = new Guid("2C7B1E6D-5F3A-4D9E-9C1F-7A6B5C2D1E88"), StatisticId = StatisticId.Speed, Actual = 70, DominantPotential = 74, RecessivePotential = 68 },
            new HorseStatistic { HorseId = new Guid("2C7B1E6D-5F3A-4D9E-9C1F-7A6B5C2D1E88"), StatisticId = StatisticId.Stamina, Actual = 72, DominantPotential = 76, RecessivePotential = 70 },
            new HorseStatistic { HorseId = new Guid("2C7B1E6D-5F3A-4D9E-9C1F-7A6B5C2D1E88"), StatisticId = StatisticId.Agility, Actual = 71, DominantPotential = 75, RecessivePotential = 69 },
            new HorseStatistic { HorseId = new Guid("2C7B1E6D-5F3A-4D9E-9C1F-7A6B5C2D1E88"), StatisticId = StatisticId.Durability, Actual = 71, DominantPotential = 75, RecessivePotential = 69 }
        );
    }
}
