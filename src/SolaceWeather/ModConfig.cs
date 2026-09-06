using StardewModdingAPI;

namespace SolaceWeather;

public sealed class ModConfig
{
    public bool EnableAbigailMemory { get; set; } = true;
    public bool EnableAbigailAi { get; set; } = true;
    public string AbigailQuestTestFarm { get; set; } = "";
    public SButton AbigailTalkKey { get; set; } = SButton.Space;
    public string GeminiModel { get; set; } = "gemini-3.8-flash";
    public SButton AbigailMemoryKey { get; set; } = SButton.F6;
    public SButton JournalKey { get; set; } = SButton.F7;
    public bool UseFahrenheit { get; set; } = true;
    public bool DeveloperMode { get; set; }
    public bool EnableArrowKeys { get; set; } = true;
    public bool EnableClickToMove { get; set; } = true;
    public bool RightClickUsesTool { get; set; } = true;
    public bool EnableSmartToolSelection { get; set; } = true;
    public bool EnableClickToInteract { get; set; } = true;
    public bool EnableClickFeedback { get; set; } = true;
    public bool EnableQuickStack { get; set; } = true;
    public SButton QuickStackKey { get; set; } = SButton.F8;
    public SButton ProtectItemKey { get; set; } = SButton.F9;
    public int QuickStackRadius { get; set; } = 6;
    public bool EnableCropProtection { get; set; } = true;
    public bool EnableMachineIndicators { get; set; } = true;
    public bool EnableNearbyCrafting { get; set; } = true;
}

