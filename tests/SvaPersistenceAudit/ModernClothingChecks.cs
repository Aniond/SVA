using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
namespace SvaPersistenceAudit;
public sealed partial class ModEntry
{
    private JsonDocument ClothingCatalog()
    {
        var art=Mod("David.AbigailModern");var helper=(StardewModdingAPI.IModHelper)Member(art,"Helper")!;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(helper.DirectoryPath,"assets/modern-clothing.json")));
    }
    private void ModernClothingChecks()
    {
        RequireOwnedWorld();using var catalog=ClothingCatalog();var checks=new Dictionary<string,bool>();var cases=new List<object>();
        var original=Game1.player;var originalRandom=Game1.random;string? error=null;Farmer? farmer=null;
        try
        {
            farmer=new Farmer();typeof(Game1).GetField("_player",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,farmer);Game1.random=new Random(180617);
            var creator=(CharacterCustomization)FormatterServices.GetUninitializedObject(typeof(CharacterCustomization));
            var shirts=creator.GetValidShirtIds();var pants=creator.GetValidPantsIds();var shop=DataLoader.Shops(Game1.content)["Sandy"];
            foreach(var entry in catalog.RootElement.GetProperty("Items").EnumerateArray())
            {
                string id=entry.GetProperty("Id").GetString()!,slot=entry.GetProperty("Slot").GetString()!,textureName=entry.GetProperty("Texture").GetString()!;int index=entry.GetProperty("SpriteIndex").GetInt32();string qualified=(slot=="shirt"?"(S)":"(P)")+id;
                var clothing=ItemRegistry.Create<Clothing>(qualified);var data=ItemRegistry.GetDataOrErrorItem(qualified);var texture=data.GetTexture();
                checks[id+"-native-item-route"]=clothing.QualifiedItemId==qualified&&data.TextureName.Replace('\\','/')==textureName&&data.SpriteIndex==index;
                checks[id+"-creator-choice"]=(slot=="shirt"?shirts:pants).Contains(id);
                checks[id+"-shop-stock"]=shop.Items.Any(i=>i.ItemId==qualified&&i.Price==1000&&i.AvailableStock==-1);
                var before=clothing.clothesColor.Value;var dye=new Color(220,85,145);clothing.Dye(dye,1);
                checks[id+"-native-dye"]=clothing.dyeable.Value&&clothing.clothesColor.Value==dye;
                if(slot=="shirt"){farmer.shirtItem.Value=clothing;farmer.shirt.Value="-1";}else{farmer.pantsItem.Value=clothing;farmer.pants.Value="-1";}
                farmer.UpdateClothing();
                if(slot=="shirt")farmer.GetDisplayShirt(out texture,out index);else farmer.GetDisplayPants(out texture,out index);
                checks[id+"-equipped-route"]=index==entry.GetProperty("SpriteIndex").GetInt32();
                bool bounds=true;
                if(slot=="shirt")for(int direction=0;direction<4;direction++)bounds&=texture.Bounds.Contains(new Rectangle(index*8%128,index*8/128*32+direction*8,8,8))&&texture.Bounds.Contains(new Rectangle(index*8%128+128,index*8/128*32+direction*8,8,8));
                else for(int body=0;body<2;body++)for(int frame=0;frame<FarmerRenderer.featureXOffsetPerFrame.Length;frame++)bounds&=texture.Bounds.Contains(new Rectangle(index%10*192+body*96+frame*16%96,index/10*688+frame*16/96*32,16,32));
                checks[id+"-all-native-source-bounds"]=bounds;
                cases.Add(new{Id=qualified,Slot=slot,Texture=texture.Name,Index=index,DefaultColor=new{before.R,before.G,before.B},Dyed=new{dye.R,dye.G,dye.B}});
            }
            checks["Exactly12AuthoredItems"]=cases.Count==12;
        }
        catch(Exception ex){error=ex.ToString();}
        finally{typeof(Game1).GetField("_player",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,original);Game1.random=originalRandom;farmer?.FarmerRenderer.unload();}
        WriteProfile("modern-clothing-checks.json",new{Passed=error==null&&checks.Values.All(v=>v),Error=error,Checks=checks,Cases=cases,Scope="Actual native ItemRegistry, creator choice methods, Sandy stock, dye/equip routes and all126 pants frames for both bodies. Fresh detached player; original appearance untouched. GPU garment visuals and persistence require separate captures/stage/sleep/load/verify."});
    }
    private void ModernClothingStage()
    {
        RequireOwnedWorld();
        string shirt="(S)David.AbigailModern_Modern_WorkdayDenim_Shirt",pants="(P)David.AbigailModern_Modern_UtilityCasual_Pants";
        var top=ItemRegistry.Create<Clothing>(shirt);var bottom=ItemRegistry.Create<Clothing>(pants);
        if(top.QualifiedItemId!=shirt||bottom.QualifiedItemId!=pants)throw new InvalidOperationException("Modern items not registered.");
        top.Dye(new Color(210,90,150),1);bottom.Dye(new Color(60,130,180),1);
        Game1.player.shirtItem.Value=top;Game1.player.pantsItem.Value=bottom;Game1.player.shirt.Value="-1";Game1.player.pants.Value="-1";Game1.player.UpdateClothing();
        WriteProfile("modern-clothing-expected.json",new{StagedUtc=DateTime.UtcNow,Shirt=shirt,Pants=pants,ShirtRgb=new[]{210,90,150},PantsRgb=new[]{60,130,180},Scope="Explicit disposable-farmer mix-and-match stage. Use native sleep/load then modern-clothing-verify; staging itself does not save."});
        namedCapture="modern-clothing-equipped.png";capturePending=true;
    }
    private void ModernClothingVerify()
    {
        RequireOwnedWorld();if(!File.Exists(SafePath("modern-clothing-expected.json")))throw new InvalidOperationException("Stage proof is required before reload verification.");
        using var expected=JsonDocument.Parse(File.ReadAllText(SafePath("modern-clothing-expected.json")));
        DateTime staged=expected.RootElement.GetProperty("StagedUtc").GetDateTime(); string folder=OwnedSaveFolder();
        bool savedAfterStage=File.GetLastWriteTimeUtc(SafePath(Path.Combine("Saves",folder,folder)))>staged;
        bool loadedAfterStage=events.Any(e=>(Member(e,"Event") as string)=="SaveLoaded"&&Member(e,"Utc") is DateTime utc&&utc>staged);
        var top=Game1.player.shirtItem.Value;var pants=Game1.player.pantsItem.Value;
        bool pass=top?.QualifiedItemId=="(S)David.AbigailModern_Modern_WorkdayDenim_Shirt"&&pants?.QualifiedItemId=="(P)David.AbigailModern_Modern_UtilityCasual_Pants"&&top.clothesColor.Value==new Color(210,90,150)&&pants.clothesColor.Value==new Color(60,130,180);
        WriteProfile("modern-clothing-reloaded.json",new{Passed=pass&&savedAfterStage&&loadedAfterStage,EquipmentMatches=pass,SavedAfterStage=savedAfterStage,NativeSaveLoadedAfterStage=loadedAfterStage,Shirt=top?.QualifiedItemId,Pants=pants?.QualifiedItemId,Day=Game1.Date.TotalDays,Scope="Native equipment/dye values plus disposable save timestamp and recorded native SaveLoaded event after staging."});
    }
}
