
using JsonDict = System.Collections.Generic.IDictionary<string, object>;

namespace KitchenDesign.Server
{
    public struct UserClientQuote
    {
        public int id;
        public string saveName;

        public string saveNotes;

        public string pdfEndpoint;

        public string coverImageName;
        public string topDownImageName;
        public string[] frontImageNames;
        public JsonDict saveData;
    }
}
