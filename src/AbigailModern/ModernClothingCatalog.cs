using System.Text.RegularExpressions;
namespace AbigailModern;

public sealed class ModernClothingCatalog
{
    public int SchemaVersion {get;set;}=1;
    public ModernClothingTexture[] Textures {get;set;}=Array.Empty<ModernClothingTexture>();
    public ModernClothingItem[] Items {get;set;}=Array.Empty<ModernClothingItem>();
    public void Validate()
    {
        if(SchemaVersion!=1 || Items.Length is <1 or >1000 || Textures.Length is <1 or >100)throw new InvalidDataException("Unsupported clothing catalog size/version.");
        if(Items.Select(i=>i.Id).Distinct(StringComparer.Ordinal).Count()!=Items.Length || Textures.Select(t=>t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Textures.Length)throw new InvalidDataException("Duplicate clothing or texture IDs.");
        foreach(var texture in Textures)
            if(!texture.Name.StartsWith("Characters/Farmer/David.AbigailModern_",StringComparison.Ordinal)||texture.Width<=0||texture.Height<=0||Path.IsPathRooted(texture.File)||texture.File.Split('/','\\').Contains("..")||!texture.File.EndsWith(".png",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid clothing texture contract.");
        foreach(var item in Items)
        {
            if(!item.Id.StartsWith("David.AbigailModern_Modern_",StringComparison.Ordinal)||!Regex.IsMatch(item.Id,"^[A-Za-z0-9_.]+$")||item.Slot is not ("shirt" or "pants")||string.IsNullOrWhiteSpace(item.DisplayName)||string.IsNullOrWhiteSpace(item.Description)||item.SpriteIndex<0||item.Price<0||item.Price>100000||item.Shop!="Sandy")throw new InvalidDataException("Invalid clothing item: "+item.Id);
            var rgb=item.DefaultColor.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(rgb.Length!=3||rgb.Any(c=>!byte.TryParse(c,out _)))throw new InvalidDataException("Invalid default clothing color: "+item.Id);
            var texture=Textures.SingleOrDefault(t=>t.Name==item.Texture)??throw new InvalidDataException("Missing clothing texture: "+item.Id);
            if(item.Slot=="shirt" && (texture.Width!=256 || texture.Height%32!=0 || item.SpriteIndex/16*32+32>texture.Height))throw new InvalidDataException("Shirt requires native128-column plane plus dye plane.");
            if(item.Slot=="pants" && (texture.Width%192!=0 || texture.Height%688!=0 || item.SpriteIndex%10*192+192>texture.Width || item.SpriteIndex/10*688+688>texture.Height))throw new InvalidDataException("Pants bank outside native layout.");
            item.Fashion.Validate(item.Slot);
        }
    }
}
public sealed class ModernClothingTexture {public string Name {get;set;}="";public string File {get;set;}="";public int Width {get;set;}public int Height {get;set;}}
public sealed class ModernClothingItem
{
    public string Id {get;set;}="";public string Slot {get;set;}="shirt";public string DisplayName {get;set;}="";public string Description {get;set;}="";public string Texture {get;set;}="";public int SpriteIndex {get;set;}
    public bool Dyeable {get;set;}=true;public bool HasSleeves {get;set;}=true;public string DefaultColor {get;set;}="255 255 255";public int Price {get;set;}=1000;public string Shop {get;set;}="Sandy";public ModernFashionMetadata Fashion {get;set;}=new();
    public string QualifiedId => (Slot=="shirt"?"(S)":"(P)")+Id;
}
public sealed class ModernFashionMetadata
{
    public string Slot {get;set;}="shirt";public int FashionValue {get;set;}=50;public string[] StyleTags {get;set;}=Array.Empty<string>();public string[] PaletteTags {get;set;}=Array.Empty<string>();public int Formality {get;set;}public int Practicality {get;set;}public int Statement {get;set;}
    public void Validate(string slot)
    {
        if(Slot!=slot||FashionValue is <0 or >100||Formality is <0 or >3||Practicality is <0 or >3||Statement is <0 or >3||!Tags(StyleTags)||!Tags(PaletteTags))throw new InvalidDataException("Invalid fashion metadata.");
    }
    private static bool Tags(string[] tags)=>tags!=null&&tags.Length<=12&&tags.All(t=>t.Length is >0 and <=40&&Regex.IsMatch(t,"^[a-z0-9-]+$"));
}
