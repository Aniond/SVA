using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Menus;
namespace SvaPersistenceAudit;
public sealed partial class ModEntry
{
    private object? proofPhone,proofState,proofConfig;
    private bool proofAi,proofPhoneEnabled;
    private IClickableMenu? proofParent;
    private IKeyboardSubscriber? proofKeyboard;
    private string? previousPhonePortraitHash;
    private void PhonePortraitProof()
    {
        RequireOwnedWorld();if(Game1.IsMultiplayer)throw new InvalidOperationException("Phone portrait proof requires owned single-player farm.");
        if(proofPhone!=null)throw new InvalidOperationException("Close the existing phone proof first.");
        var mod=Mod("David.SolaceWeather");var phone=Member(mod,"phone")??throw new InvalidOperationException("Phone service missing.");
        var portraits=PortraitService();long id=Game1.player.UniqueMultiplayerID;
        if(Member(phone,"pending")!=null||Member(portraits,"pending")!=null)throw new InvalidOperationException("Proof cannot run with an active provider request.");
        string status=(string)portraits.GetType().GetMethod("GetStatus")!.Invoke(portraits,new object[]{id})!;
        if(status!="Ready")throw new InvalidOperationException("Proof requires an existing Ready portrait; it never requests one.");
        var phoneMenuType=mod.GetType().Assembly.GetType("SolaceWeather.Relationships.PhoneMenu")!;
        var lookup=(Delegate?)phoneMenuType.GetProperty("PlayerPortraits",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)??throw new InvalidOperationException("PhoneMenu.PlayerPortraits is not wired.");
        object?[] sharedArgs={id,null,null},phoneArgs={id,null,null};
        bool shared=(bool)portraits.GetType().GetMethod("TryGetPortrait")!.Invoke(portraits,sharedArgs)!;
        bool available=(bool)lookup.DynamicInvoke(phoneArgs)!;
        var texture=phoneArgs[1] as Texture2D;var source=phoneArgs[2] is Rectangle r?r:Rectangle.Empty;
        var cache=(PlayerPortraitCache?)Member(portraits,"cache");var record=(PlayerPortraitRecord?)Member(portraits,"record");
        string hash=cache!=null&&File.Exists(cache.ImagePath)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(cache.ImagePath))).ToLowerInvariant():"";
        bool exact=shared&&available&&ReferenceEquals(sharedArgs[1],texture)&&texture is {Width:1024,Height:1024}&&source==texture.Bounds&&hash==record?.Sha256;
        if(!exact)throw new InvalidOperationException("Phone lookup does not return the exact Ready1024 cached service texture.");
        proofPhone=phone;proofState=Member(phone,"state");proofConfig=Member(phone,"config")!;
        proofAi=(bool)proofConfig.GetType().GetProperty("EnableAbigailAi")!.GetValue(proofConfig)!;
        proofPhoneEnabled=(bool)proofConfig.GetType().GetProperty("EnablePhone")!.GetValue(proofConfig)!;
        proofParent=Game1.activeClickableMenu;proofKeyboard=Game1.keyboardDispatcher.Subscriber;
        try
        {
            proofConfig.GetType().GetProperty("EnableAbigailAi")!.SetValue(proofConfig,false);
            proofConfig.GetType().GetProperty("EnablePhone")!.SetValue(proofConfig,true);
            var staged=new PhoneState{FarmerId=id};staged.Contacts["Abigail"]=new PhoneContact{Exchanged=true};
            staged.Thread("Abigail").Messages.Add(new PhoneMessage{Text="Taking a quiet break by the lake.",Outgoing=true,Status="sent",Day=Game1.Date.TotalDays,Time=Game1.timeOfDay});
            staged.Thread("Abigail").Messages.Add(new PhoneMessage{Text="That sounds nice. See you around!",Outgoing=false,Status="sent",Day=Game1.Date.TotalDays,Time=Game1.timeOfDay});
            phone.GetType().GetField("state",Members)!.SetValue(phone,staged);
            phone.GetType().GetMethod("Open",Members)!.Invoke(phone,null);
            var menu=Game1.activeClickableMenu;
            if(menu?.GetType()!=phoneMenuType)throw new InvalidOperationException("Native phone did not open.");
            phoneMenuType.GetMethod("Select",Members)!.Invoke(menu,new object[]{"Abigail"});
            WriteProfile("phone-portrait-proof.json",new{Passed=true,Status=status,FarmerId=id,CacheSha256=hash,PreviousCaptureHash=previousPhonePortraitHash,ChangedFromPrevious=previousPhonePortraitHash!=null&&previousPhonePortraitHash!=hash,Source=source,texture!.Width,texture.Height,SharedTextureIdentity=true,LookupTarget=lookup.Method.DeclaringType?.FullName+"."+lookup.Method.Name,OutgoingCount=staged.Thread("Abigail").Messages.Count(m=>m.Outgoing),Contact=Member(menu,"Contact"),ProviderCallsRequested=0,OriginalPhoneStateSuspended=true,Scope="Actual native phone with temporary in-memory exchanged contact and sent messages; cached service lookup; no provider or save calls. Close restores exact original phone state/config/menu/keyboard; friendship never edited."});
            previousPhonePortraitHash=hash;namedCapture="phone-portrait-proof.png";capturePending=true;
        }
        catch{ClosePhonePortraitProof();throw;}
    }
    private void ClosePhonePortraitProof()
    {
        RequireOwnedWorld();if(proofPhone==null)return;
        var phone=proofPhone;
        try{phone.GetType().GetMethod("Close",Members)!.Invoke(phone,null);}
        finally
        {
            phone.GetType().GetField("state",Members)!.SetValue(phone,proofState);
            proofConfig!.GetType().GetProperty("EnableAbigailAi")!.SetValue(proofConfig,proofAi);
            proofConfig.GetType().GetProperty("EnablePhone")!.SetValue(proofConfig,proofPhoneEnabled);
            Game1.activeClickableMenu=proofParent;Game1.keyboardDispatcher.Subscriber=proofKeyboard;
            WriteProfile("phone-portrait-close.json",new{Restored=ReferenceEquals(Member(phone,"state"),proofState),ProviderPending=Member(phone,"pending")!=null,At=DateTime.UtcNow});
            proofPhone=null;proofState=null;proofConfig=null;proofParent=null;proofKeyboard=null;
        }
    }
}
