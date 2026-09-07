using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Shirts;
using StardewValley.GameData.Pants;
using StardewValley.GameData.Shops;
using Microsoft.Xna.Framework.Graphics;
namespace AbigailModern;

/// <summary>Adds independently wearable clothing through native data and Sandy's normal stock.</summary>
internal static class ModernClothingLoader
{
    internal const string FashionAsset="David.AbigailModern/FashionCatalog";
    internal static ModernClothingCatalog? Catalog {get;private set;}
    private static readonly JsonSerializerOptions FashionJson=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
    public static void Initialize(IModHelper helper,IMonitor monitor)
    {
        var catalog=helper.Data.ReadJsonFile<ModernClothingCatalog>("assets/modern-clothing.json");
        if(catalog==null){monitor.Log("Modern clothing catalog is absent; native clothing unchanged.",LogLevel.Warn);return;}
        catalog.Validate();Catalog=catalog;
        helper.Events.Content.AssetRequested+=(_,e)=>
        {
            var texture=catalog.Textures.FirstOrDefault(t=>e.NameWithoutLocale.IsEquivalentTo(t.Name));
            if(texture!=null) e.LoadFromModFile<Texture2D>(texture.File,AssetLoadPriority.Exclusive);
            else if(e.NameWithoutLocale.IsEquivalentTo("Data/Shirts"))e.Edit(asset=>
            {
                var data=asset.AsDictionary<string,ShirtData>().Data;
                foreach(var item in catalog.Items.Where(i=>i.Slot=="shirt"))
                {
                    if(data.ContainsKey(item.Id))throw new InvalidDataException("Refusing to replace an existing shirt ID: "+item.Id);
                    data.Add(item.Id,new ShirtData{Name=item.DisplayName,DisplayName=item.DisplayName,Description=item.Description,Price=item.Price,Texture=item.Texture,SpriteIndex=item.SpriteIndex,DefaultColor=item.DefaultColor,CanBeDyed=item.Dyeable,HasSleeves=item.HasSleeves,CanChooseDuringCharacterCustomization=true,CustomFields=Fields(item)});
                }
            });
            else if(e.NameWithoutLocale.IsEquivalentTo("Data/Pants"))e.Edit(asset=>
            {
                var data=asset.AsDictionary<string,PantsData>().Data;
                foreach(var item in catalog.Items.Where(i=>i.Slot=="pants"))
                {
                    if(data.ContainsKey(item.Id))throw new InvalidDataException("Refusing to replace an existing pants ID: "+item.Id);
                    data.Add(item.Id,new PantsData{Name=item.DisplayName,DisplayName=item.DisplayName,Description=item.Description,Price=item.Price,Texture=item.Texture,SpriteIndex=item.SpriteIndex,DefaultColor=item.DefaultColor,CanBeDyed=item.Dyeable,CanChooseDuringCharacterCustomization=true,CustomFields=Fields(item)});
                }
            });
            else if(e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))e.Edit(asset=>
            {
                var shops=asset.AsDictionary<string,ShopData>().Data;
                foreach(var group in catalog.Items.GroupBy(i=>i.Shop))
                {
                    if(!shops.TryGetValue(group.Key,out var shop))throw new InvalidDataException("Native clothing shop missing: "+group.Key);
                    foreach(var item in group) if(!shop.Items.Any(s=>s.Id==item.Id))shop.Items.Add(new ShopItemData{Id=item.Id,ItemId=item.QualifiedId,Price=item.Price,AvailableStock=-1});
                }
            });
            else if(e.NameWithoutLocale.IsEquivalentTo(FashionAsset))e.LoadFrom(()=>JsonSerializer.Serialize(new {schemaVersion=1,items=catalog.Items.ToDictionary(i=>i.QualifiedId,i=>i.Fashion)},FashionJson),AssetLoadPriority.Exclusive);
        };
        monitor.Log($"Modern clothing ready: {catalog.Items.Length} additive choices in character creation and Sandy's shop.",LogLevel.Info);
    }
    private static Dictionary<string,string> Fields(ModernClothingItem item)=>new(){{"David.AbigailModern/Fashion",JsonSerializer.Serialize(item.Fashion,FashionJson)}};
}
