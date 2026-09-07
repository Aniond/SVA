using System.Text.Json;
using AbigailModern;
string json=File.ReadAllText("src/AbigailModern/assets/modern-clothing.json");
ModernClothingCatalog Read()=>JsonSerializer.Deserialize<ModernClothingCatalog>(json)!;
void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS: "+label);}
var catalog=Read();catalog.Validate();Check(catalog.Items.Count(i=>i.Slot=="shirt")==6&&catalog.Items.Count(i=>i.Slot=="pants")==6,"six independently identified shirts and pants");
Check(catalog.Items.All(i=>i.Dyeable&&i.Price==1000&&i.Shop=="Sandy"),"native dye and existing shop acquisition contracts");
Check(catalog.Items.Select(i=>i.QualifiedId).Distinct().Count()==12,"permanent qualified IDs are unique");
void Reject(Action<ModernClothingCatalog> mutate,string label){var c=Read();mutate(c);bool bad=false;try{c.Validate();}catch(InvalidDataException){bad=true;}Check(bad,label);}
Reject(c=>c.Items[1].Id=c.Items[0].Id,"duplicate stable IDs rejected");
Reject(c=>c.Items.First(i=>i.Slot=="pants").SpriteIndex=6,"out-of-bounds pants animation bank rejected");
Reject(c=>c.Items.First(i=>i.Slot=="shirt").SpriteIndex=16,"out-of-bounds shirt direction bank rejected");
Reject(c=>c.Items[0].Fashion.StyleTags=new[]{"Upper_Case"},"fashion schema tag violations rejected");
Reject(c=>c.Items[0].DefaultColor="999 0 0","invalid native RGB rejected");
Reject(c=>c.Textures[0].File="../outside.png","escaping content path rejected");
Check(catalog.Items.All(i=>i.Fashion.Slot==i.Slot),"fashion slots match native equipment slots");
