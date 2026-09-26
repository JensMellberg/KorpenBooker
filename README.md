# Korpen booker
Boka korpenpass i pingis utan att behöva vara snabb som en hök!
Det är en konsollapp så kör den genom terminalen, den styrs med några kommandon som alla är på formen `-<command> <param1> <param2>…`. T.ex `-list 2026-09-25 2026-09-28` för att lista pass mellan 25e och 28e september.

## Ladda ner

Orkar du inte kompilera koden själv är det bara ladda ner .exe'n här
[⬇️ Ladda ner senaste versionen](../../releases/latest/download/KorpenBooker.exe)

## Alla kommandon

### -session <sessionskaka>

Sparar ner sessionskakan till korpensidan. Den behövs för bokningsanropet och ska vara din personliga. Du hittar den i **dev-tools** (F12 på chrome), under fliken **Application** och menyalternativet **Cookies**. Kakan heter **session** och dess **Value** ska vara en lång slumpmässig sträng.

### -userid <userid>

Sparar ner ditt korpen-användarid. Det behövs för bokningsanropet, det kommer sättas automatiskt om du angett en giltig sessionskaka

### -list <frånDatum> <tillDatum>

Listar alla pass och deras träningsid'n i tidsintervallet, träningsid't behövs för att göra anropet för bokningen.

### -book <träningsid>

Lägger in en schemalagd task i windows taskscheduler som kommer utföra bokningsanropet 5 dagar innan passet, är passet redan bokningsbart kommer den boka direkt. <träningsid> får du genom att köra `-list`-kommandot

### -exit

Avslutar applikationen

