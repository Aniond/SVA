namespace SolaceWeather.Core;

/// <summary>Authored tastes, not claims about native friendship or an NPC's unseen thoughts.</summary>
public sealed record NpcFashionProfile(NpcFashionTaste Taste, string Compliment, string Dislike);
public static class NpcFashionProfiles
{
    private static NpcFashionProfile P(int standard, int importance, int formal, int practical, int statement, string tags, string positive, string negative)
        => new(new() { Standard = standard, Importance = importance, Formality = formal, Practicality = practical, Statement = statement,
            PreferredTags = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries) }, positive, negative);
    public static IReadOnlyDictionary<string, NpcFashionProfile> All { get; } = new Dictionary<string, NpcFashionProfile>(StringComparer.Ordinal)
    {
        ["Haley"] = P(65,3,1,1,2,"polished modern streetwear", "Okay, that outfit actually works really well on you. Seriously.", "Hmm. That wouldn't be my first choice, but you're the one wearing it."),
        ["Abigail"] = P(50,2,0,2,2,"alternative playful outdoors", "I like that look. It has a little personality to it.", "It's not really my kind of outfit. I'd want something I could move around in."),
        ["Alex"] = P(55,2,0,3,1,"sporty casual", "Looking good. That outfit works for you.", "I'd go with something a little more relaxed myself."),
        ["Elliott"] = P(60,2,3,1,2,"classic tailored", "That is a rather well-chosen ensemble.", "I confess I favor a different sort of style, but we needn't all dress alike."),
        ["Emily"] = P(50,3,1,1,3,"creative colorful bohemian handmade craft expressive", "I love seeing you try a look with its own character!", "I think I'd put those pieces together a little differently. Everyone has their own eye."),
        ["Harvey"] = P(55,1,2,2,0,"classic understated", "You look very put together today.", "I usually choose something a little more understated for myself."),
        ["Leah"] = P(50,2,0,2,2,"natural artistic casual", "I like the feel of that outfit. It looks like you chose it for yourself.", "I think I'd be more comfortable in something a bit simpler."),
        ["Maru"] = P(50,1,0,3,1,"practical utilitarian", "That looks like a useful, comfortable combination.", "I'd probably pick something more practical for my own day."),
        ["Penny"] = P(50,1,1,2,0,"understated classic", "That is a lovely outfit on you.", "It's a little different from what I'd choose for myself."),
        ["Sam"] = P(50,2,0,2,2,"casual streetwear sporty", "Hey, nice outfit. I like that look.", "I'd probably wear something more casual. That's just me, though."),
        ["Sebastian"] = P(50,2,0,2,2,"dark alternative streetwear", "That look works. I like it.", "Not really my style, but you don't need my permission."),
        ["Shane"] = P(35,1,0,3,0,"workwear practical", "Looks comfortable enough. That's most of what I care about.", "I wouldn't pick it for myself. Clothes aren't a big thing for me."),
        ["Caroline"] = P(50,1,1,2,1,"relaxed natural", "That outfit suits you nicely.", "I lean toward a more relaxed look myself."),
        ["Clint"] = P(40,1,0,3,0,"workwear practical", "Those clothes look sensible. I appreciate that.", "I usually worry more about whether my clothes will stand up to work."),
        ["Demetrius"] = P(45,1,0,3,0,"practical utilitarian", "That seems like a practical choice of clothing.", "I tend to choose clothes for what I'll be doing that day."),
        ["Evelyn"] = P(50,2,2,1,1,"classic colorful", "What a lovely outfit, dear.", "I might choose something a little more traditional, but styles do change."),
        ["George"] = P(45,1,1,3,0,"classic workwear", "Now those look like sensible clothes.", "Not what I'd wear. I suppose everyone has their own taste."),
        ["Gus"] = P(50,1,1,2,1,"classic practical", "You're looking sharp today.", "I'd choose something a little different myself. Make yourself comfortable."),
        ["Jodi"] = P(50,1,1,3,0,"casual practical", "That looks like a nice outfit for the day.", "I'd probably choose something simpler for running around town."),
        ["Kent"] = P(45,1,0,3,0,"practical utilitarian", "That looks like a sensible choice.", "I tend to keep my own clothes simple."),
        ["Lewis"] = P(55,1,2,2,0,"classic tailored", "You're looking quite presentable today.", "I admit I prefer a more traditional look."),
        ["Linus"] = P(30,1,0,3,0,"natural practical", "Those clothes seem well chosen for you.", "I choose clothes for comfort more than appearance."),
        ["Marnie"] = P(45,1,0,3,1,"country practical", "I like that outfit. It seems to suit you.", "I'd choose something more practical for my own day."),
        ["Pam"] = P(35,1,0,3,0,"casual workwear", "Hey, that outfit looks all right on you.", "Not my sort of thing. Wear what you like."),
        ["Pierre"] = P(55,1,2,2,0,"classic polished", "You're looking well put together.", "I personally prefer a more conventional style."),
        ["Robin"] = P(45,2,0,3,1,"workwear outdoors", "I like that. A good outfit doesn't have to be fussy.", "I'd want something more practical for my own work."),
        ["Sandy"] = P(60,3,1,1,3,"colorful modern statement", "Oh, that's a look I can appreciate!", "I think I'd choose something with a different feel. Style is personal, isn't it?"),
        ["Willy"] = P(40,1,0,3,0,"workwear outdoors", "Looks like you've chosen your clothes well.", "I lean toward useful clothes myself. That's all."),
        ["Wizard"] = P(50,1,2,1,3,"mystical dramatic", "Your choice of attire has a certain presence.", "My own taste in attire is rather different."),
        ["Krobus"] = P(40,1,0,2,0,"dark understated", "I like your clothes. I hope that is all right to say.", "I do not understand every human style. I think I prefer simpler clothes."),
        ["Dwarf"] = P(45,1,0,3,2,"practical utilitarian", "Those clothes look useful. A good choice.", "Human clothing is strange to me. I prefer something practical."),
        ["Jas"] = P(45,1,1,1,2,"colorful playful", "I like your outfit.", "I think I'd pick different clothes for me."),
        ["Vincent"] = P(40,1,0,2,3,"playful sporty", "Whoa, I like those clothes!", "I'd pick something different. Something fun."),
        ["Leo"] = P(35,1,0,3,1,"natural outdoors", "I like your clothes.", "I like clothes that are easy to move in."),
        ["Gunther"] = P(55,1,2,2,0,"classic tailored", "A well-considered choice of attire.", "My own tastes are rather more traditional."),
        ["Marlon"] = P(40,1,0,3,0,"outdoors practical", "Looks like a sensible outfit.", "I usually choose my clothes for their usefulness."),
        ["Gil"] = P(35,1,0,3,0,"practical workwear", "Looks comfortable.", "I prefer something simpler."),
        ["Birdie"] = P(40,1,0,3,1,"natural casual", "Those clothes suit you well enough.", "I choose something simple for myself these days."),
        ["MrQi"] = P(65,2,3,1,3,"polished dramatic", "An interesting choice of presentation.", "Not quite my taste. But taste is a personal matter.")
    };
    public static NpcFashionProfile? Get(string name) => All.GetValueOrDefault(name);
}
