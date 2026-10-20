using System.Collections.Generic;
using UnityEngine;

namespace DoomsdayTunnels.Story
{
    public sealed class Line { public string Who, Text, Fx; public Line(string who, string text, string fx = null) { Who = who; Text = text; Fx = fx; } }

    /// <summary>One shot of a cut scene: a set, the lines said in it, and the set's options.</summary>
    public sealed class Shot
    {
        public string Bg; public Line[] Lines;
        public string Title, Jingle;
        public bool Moon, Mother, NoBaron, Cut, Dot;
    }

    public struct Speaker { public string Name; public Color Col; public float Pitch; }

    /// <summary>The story, sector by sector, exactly as the web game tells it.</summary>
    public static class Script
    {
        public static readonly Dictionary<string, Speaker> Who = new Dictionary<string, Speaker>
        {
            ["narrator"] = new Speaker { Name = "NARRATOR", Col = K.White, Pitch = 520 }, ["baron"] = new Speaker { Name = "BARON VON KABOOM", Col = K.Pink, Pitch = 150 },
            ["general"] = new Speaker { Name = "GENERAL GRANITE", Col = K.Yellow, Pitch = 240 }, ["ace"] = new Speaker { Name = "ACE CHASE", Col = K.Cyan, Pitch = 400 },
            ["buddy"] = new Speaker { Name = "B.U.D.D.Y.", Col = K.Green, Pitch = 900 }, ["cat"] = new Speaker { Name = "MISTER FLUFFLES", Col = K.Orange, Pitch = 1300 },
            ["mother"] = new Speaker { Name = "MOTHER VON KABOOM", Col = K.H("#e58bff"), Pitch = 310 }, ["debbie"] = new Speaker { Name = "DEBBIE, PRESTIGE ROCKET RENTAL", Col = K.H("#9fd0ff"), Pitch = 700 }
        };

        static Line L(string who, string text, string fx = null) => new Line(who, text, fx);
        static Shot S(string bg, Line[] lines, string title = null, string jingle = null, bool moon = false, bool mother = false, bool noBaron = false, bool cut = false, bool dot = false)
            => new Shot { Bg = bg, Lines = lines, Title = title, Jingle = jingle, Moon = moon, Mother = mother, NoBaron = noBaron, Cut = cut, Dot = dot };

        public static Shot[] Intro() => new[]
        {
            S("card", new[] { L("narrator", "The year is 1999. The future! Hair is enormous. Computers have up to sixteen colours. The world is at peace.", "sting") }, title: "1999 A.D."),
            S("lair", new[] {
                L("baron", "Peace? PEACE?! I, Baron Von Kaboom, did not spend nine years at Evil Medical School for PEACE!", "thunder"),
                L("baron", "Behold, my DOOMSDAY DEVICE! Buried at the centre of the Earth, where nobody can unplug it!"),
                L("baron", "MWAH-HA-HA-HA! Laugh with me, Mister Fluffles!", "laugh"),
                L("cat", "Meow.") }, jingle: "villain"),
            S("hq", new[] {
                L("general", "Ace! Thank goodness you picked up. The Baron's device goes off in minutes."),
                L("general", "The only way in is his network of colour-coded tunnels. They are full of walls. We do not know why."),
                L("general", "One more thing. The rental company has hidden a SHIP TOKEN in every sector. Grab it, and a spare ship will be waiting at the end.") }, jingle: "morse"),
            S("cockpit", new[] {
                L("ace", "Walls, General? I eat walls for breakfast."),
                L("general", "Please don't. The ship is a rental."),
                L("buddy", "GOOD MORNING, ACE. I AM B.U.D.D.Y., YOUR ON-BOARD COMPUTER. I HAVE TAKEN THE LIBERTY OF UPDATING YOUR WILL.") }, jingle: "hero"),
            S("launch", new[] { L("narrator", "And so, with the fate of the Earth and one security deposit on the line... ACE CHASE flies!", "fanfare") })
        };

        /// <summary>The scene played before sector n, or null when there is none (sector 1 has the intro instead).</summary>
        public static Shot[] For(int n)
        {
            switch (n)
            {
                case 2: return new[] { S("lair", new[] {
                    L("baron", "WHAT?! He survived the Cyan Sector? Nobody survives the Cyan Sector! Admittedly, nobody had tried.", "thunder"),
                    L("baron", "No matter. The VIOLET Sector is twice as deadly. And twice as violet."),
                    L("cat", "Meow."),
                    L("baron", "Not now, Mister Fluffles. Daddy is monologuing.") }, jingle: "villain") };
                case 3: return new[] {
                    S("hq", new[] {
                        L("general", "Fine flying, Ace. Our eggheads have decoded the Baron's blueprints."),
                        L("general", "Bad news: the next sector is green. Worse news: he has added more mines.") }, jingle: "morse"),
                    S("cockpit", new[] {
                        L("ace", "Mines? General, the only thing I mine... is DANGER."),
                        L("general", "That does not make any sense, son."),
                        L("ace", "It didn't have to.") }, jingle: "hero") };
                case 4: return new[] {
                    S("lair", new[] {
                        L("baron", "They laughed at me at the Academy of Tunnel Science! \"Tunnels should GO somewhere, Kaboom,\" they said!", "thunder"),
                        L("baron", "Well, who is laughing now? ME! Observe: MWAH-HA-HA-HA-", "laugh"),
                        L("baron", "-ha. Ahem. Dry throat. RELEASE THE PINK SECTOR!") }, jingle: "villain"),
                    S("buddy", new[] {
                        L("buddy", "ACE. I CALCULATE A THREE PERCENT CHANCE OF SURVIVAL."),
                        L("ace", "Buddy, I didn't ask for math. I asked for THRUST."),
                        L("buddy", "THRUST IS ALSO MATH, ACE.") }) };
                case 5: return new[] {
                    S("hq", new[] {
                        L("general", "Ace, you are almost at the core. One sector left: the blue one."),
                        L("general", "If you don't make it back... I want you to know the rental company WILL be billing your estate.") }, jingle: "morse"),
                    S("lair", new[] {
                        L("baron", "Blue was my mother's favourite colour. She never believed in me either."),
                        L("baron", "FULL POWER TO THE WALLS!", "thunder") }, jingle: "villain") };
                case 6: return new[] {
                    S("device", new[] {
                        L("narrator", "The Doomsday Device! Seven seconds on the clock!", "sting"),
                        L("buddy", "ACE. CUT THE RED WIRE. OR POSSIBLY THE BLUE WIRE. I HAVE MISPLACED THE MANUAL."),
                        L("ace", "No time, Buddy. I'll cut... BOTH.") }),
                    S("device", new[] {
                        L("narrator", "It worked. It should not have worked.", "snip"),
                        L("general", "Outstanding, Ace! The Earth is saved, and the rental is only mostly on fire!", "fanfare") }, cut: true),
                    S("pod", new[] {
                        L("baron", "Curse you, Ace Chase! You have not seen the last of Baron Von Kaboom!"),
                        L("baron", "I HAVE A SPARE!", "thunder"),
                        L("cat", "Meow.") }, jingle: "villain"),
                    S("card", new[] { L("narrator", "He did have a spare. He kept it somewhere safe. He kept it... on the Moon.", "sting") }, title: "THE END?"),
                    S("card", new[] { L("narrator", "DOOMSDAY TUNNELS, PART TWO: ELECTRIC KABOOM-ALOO. This time it is personal. It is also on the Moon.", "fanfare") }, title: "PART II", moon: true),
                    S("lair", new[] {
                        L("baron", "Welcome to my MOON BASE, Mister Fluffles! The spare Doomsday Device is buried deep inside the Moon. Which, it turns out, is cheese."),
                        L("mother", "Wolfgang! Are you monologuing to the cat again?"),
                        L("baron", "MOTHER! Not in front of my nemesis!"),
                        L("cat", "Meow.") }, jingle: "villain", moon: true, mother: true),
                    S("hq", new[] {
                        L("general", "Ace, bad news. The Baron has tunnelled into the Moon. Worse news: the Moon is made of cheese. Science is very upset."),
                        L("general", "Budget cuts mean you are flying the same rental. Try not to scratch the Moon.") }, jingle: "morse"),
                    S("cockpit", new[] {
                        L("ace", "General, I was BORN to fly through cheese."),
                        L("general", "Nobody is born for that, son."),
                        L("buddy", "ACE. I HAVE INSTALLED A FEELINGS MODULE. I FEEL... CONCERNED.") }, jingle: "hero") };
                case 7: return new[] { S("lair", new[] {
                    L("mother", "Wolfgang, these tunnels are the same colours as last time."),
                    L("baron", "It is called a BRAND, Mother!"),
                    L("mother", "It is called lazy. Your cousin Gunther has a volcano."),
                    L("cat", "Meow.") }, jingle: "mother", moon: true, mother: true) };
                case 8: return new[] { S("rental", new[] {
                    L("debbie", "Hi! This is Debbie from Prestige Rocket Rental. Our records show the vehicle has left the planet. That is a different mileage plan."),
                    L("ace", "Debbie, I'm a little busy saving the world."),
                    L("debbie", "I can see that, and I would LOVE to help you with it! There is a nine dollar fee.") }, jingle: "phone") };
                case 9: return new[] { S("lair", new[] {
                    L("baron", "Behold the true power of the spare! It will melt the Moon into the largest FONDUE in history and drop it on the Earth!", "thunder"),
                    L("mother", "With what bread, Wolfgang? Did you think about the bread?"),
                    L("baron", "...I did not think about the bread."),
                    L("mother", "Nine years of Evil Medical School, and not one thought about bread.") }, jingle: "villain", moon: true, mother: true) };
                case 10: return new[] {
                    S("hq", new[] {
                        L("general", "Ace, intel says the Baron's mother is up there with him. Stay sharp. I served with that woman."),
                        L("general", "She once grounded an entire air force. Then she grounded them again, for sulking.") }, jingle: "morse"),
                    S("buddy", new[] {
                        L("buddy", "ACE. MY FEELINGS MODULE REPORTS A NEW FEELING. I BELIEVE IT IS CALLED \"MONDAY\"."),
                        L("ace", "That's just dread, Buddy. You get used to it."),
                        L("buddy", "I WOULD PREFER NOT TO. PLEASE FLY FASTER.") }) };
                case 11: return new[] {
                    S("fondue", new[] {
                        L("narrator", "The core of the Moon! The spare Doomsday Device! And this time there are no wires at all!", "sting"),
                        L("buddy", "ACE. THERE IS ONLY A BUTTON. THE LABEL SAYS \"DO NOT\"."),
                        L("ace", "Do not WHAT, Buddy?"),
                        L("buddy", "THE REST OF THE LABEL HAS BEEN CHEWED OFF.") }),
                    S("catboss", new[] {
                        L("cat", "Meow.", "thunder"),
                        L("buddy", "TRANSLATING. HE SAYS HE HAS RUN THIS ENTIRE OPERATION SINCE 1987. THE BARON WAS, QUOTE, \"THE FACE\"."),
                        L("baron", "Fluffles?! After everything Mother and I did for you?"),
                        L("mother", "I always said that cat had ambition, Wolfgang. Unlike some people.") }, jingle: "villain"),
                    S("cockpit", new[] {
                        L("ace", "There's one thing no cat in the galaxy can resist, Buddy. Fire the laser. At the wall."),
                        L("buddy", "ACE. THAT IS THE SMARTEST THING YOU HAVE EVER SAID. I FEEL... PROUD. IT IS ALSO UNPLEASANT.") }, jingle: "hero"),
                    S("fondue", new[] {
                        L("narrator", "And so the Moon was saved by one small red dot. Mister Fluffles chased it for eleven hours.", "fanfare"),
                        L("general", "Outstanding, Ace! The Moon is saved, the cheese is contained, and Debbie says your deposit is \"under review\".") }, dot: true),
                    S("card", new[] { L("narrator", "But far away, cousin Gunther looked up from his volcano, and smiled. Sector 11 awaits.", "sting") }, title: "THE END??", moon: true) };
                default: return n > 11 ? Later(n) : null;
            }
        }

        /// <summary>After sector 11 the generic scenes rotate.</summary>
        public static Shot[] Later(int n)
        {
            switch ((n - 12) % 7)
            {
                case 0: return new[] { S("lair", new[] { L("baron", "Another device, another sector! Did I mention I bought in bulk?", "thunder"), L("cat", "Meow.") }, jingle: "villain") };
                case 1: return new[] { S("hq", new[] { L("general", "Ace, the Baron is on device number " + (n - 4) + ". Frankly, we are impressed by his budget.") }, jingle: "morse") };
                case 2: return new[] { S("buddy", new[] { L("buddy", "ACE. SURVIVAL ODDS NOW ROUND DOWN TO ZERO. I BELIEVE IN YOU ANYWAY."), L("ace", "That's the nicest thing you've ever calculated.") }) };
                case 3: return new[] { S("lair", new[] { L("baron", "Mister Fluffles thinks you will crash in this one. He is rarely wrong."), L("cat", "Meow.") }, jingle: "villain") };
                case 4: return new[] { S("cockpit", new[] { L("ace", "Sector " + n + "? I've had hair appointments longer than this."), L("general", "We know, son. We have seen the invoices.") }, jingle: "hero") };
                case 5: return new[] { S("lair", new[] { L("mother", "Wolfgang has gone to his room. I am running the tunnels now. Sit up straight, pilot."), L("cat", "Meow.") }, jingle: "mother", moon: true, mother: true, noBaron: true) };
                default: return new[] { S("rental", new[] { L("debbie", "Hi, Debbie again! Courtesy call: the vehicle is now " + n * 1000 + " sectors over its mileage plan. Have a great day!") }, jingle: "phone") };
            }
        }
    }

    /// <summary>One-liners for the screens between runs.</summary>
    public static class Quips
    {
        public static readonly string[]
            Time = { "The Earth has been cancelled. Refunds are unavailable.", "Baron Von Kaboom wins. Mister Fluffles is being insufferable about it.", "On the bright side, nobody has to return the rental." },
            Ship = { "Ace ejected safely. The security deposit did not.", "The rental company has been notified.", "General Granite is drafting a very stern memo." },
            SpareShip = { "The rental company is sending another one.", "B.U.D.D.Y. kept a spare in the trunk.", "Ace ejected with style. Fresh ship inbound." },
            SpareTime = { "The Baron hit snooze. Back to the start of the sector.", "A suspiciously convenient clock malfunction.", "Mister Fluffles sat on the reset button." },
            PbTime = { "Your fastest yet. B.U.D.D.Y. has revised your odds. Upward, for once.", "A personal best. General Granite very nearly smiled.", "Faster than ever. The rental company would like a word about the engine." },
            Record = { "The Baron timed that sector himself. He demands a recount.", "One for the record book. Mister Fluffles knocked the stopwatch off the desk.",
                "General Granite is naming a sandwich after you.", "Debbie says speed records are not covered by the rental agreement." },
            BestScore = { "Your best run yet. General Granite is framing the score. And the invoice.", "A new personal best. Mister Fluffles pretends not to care.", "Your highest score ever. The rental company is billing you anyway." },
            BoardTop = { "Number one. The Baron has written to the scoreboard to complain.", "Top of the board. Mister Fluffles has demanded to see a manager." },
            Board = { "General Granite has pinned your score to the fridge.", "B.U.D.D.Y. has printed the scoreboard. He does not have a printer. Nobody knows how.", "The Baron is pretending he has not looked at the board. He has looked." };
        public static string Pick(string[] a) => a[Random.Range(0, a.Length)];
    }
}
