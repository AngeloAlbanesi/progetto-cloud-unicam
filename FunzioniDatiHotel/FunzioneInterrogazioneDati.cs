using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FunzioniDatiHotel.Modelli;

namespace FunzioniDatiHotel
{
    public class FunzioneInterrogazioneDati
    {


        public static async Task<Hotel[]> DaiElencoHotel()
        {

            string BaseUri = "http://www.datiopen.it//ODataProxy/MdData('8fbb00cb-943e-4141-8e7e-758d6c292778@datiopen')/DataRows?$skip=0&$top=2000";
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(BaseUri);
            string contents = await response.Content.ReadAsStringAsync();
            XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
            XNamespace atom = "http://www.w3.org/2005/Atom";  // Namespace di Atom per l'elemento "entry"


            XDocument xml = XDocument.Parse(contents);
            List<Hotel> hotels = new List<Hotel>();
           

            foreach (var entry in xml.Descendants(atom + "entry"))
            {
                var contentElement = entry.Element(atom + "content");
                if (contentElement != null)
                {
                    var properties = contentElement.Element(m + "properties");
                    if (properties != null)
                    {
                        // Verifica se properties contiene degli elementi
                        Console.WriteLine("Properties trovate!");

                        // Prova a stampare i valori dei singoli elementi per vedere se sono correttamente estratti
                        Console.WriteLine("Comune: " + properties.Element(d + "Cccomune_608711150")?.Value);
                        Console.WriteLine("Denominazione: " + properties.Element(d + "Ccdenominazione1291548260")?.Value);
                        Console.WriteLine("Tipologia: " + properties.Element(d + "Cctipologia_alb1238256790")?.Value);

                        hotels.Add(new Hotel
                        {
                            Comune = properties.Element(d+"Cccomune_608711150")?.Value ?? "Sconosciuto",
                            DenominazioneStruttura = properties.Element(d+"Ccdenominazione1291548260")?.Value ?? "Sconosciuto",
                            Tipologia = properties.Element(d+"Cctipologia_alb1238256790")?.Value ?? "Sconosciuto"
                        });
                    }
                    else
                    {
                        Console.WriteLine("Properties non trovate!");
                    }
                }
                else
                {
                    Console.WriteLine("Content non trovato!");
                }
            }




            return hotels.ToArray();
            
        }

       
    }


}
