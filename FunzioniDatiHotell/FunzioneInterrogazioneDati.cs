using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FunzioniDatiHotell.Modelli;

namespace FunzioniDatiHotell
{
    
    public class FunzioneInterrogazioneDati
    {
        private static string BaseUrl;

        public static void SetBaseUrl(string url)
        {
            BaseUrl = url;
        }

        public static async Task<Hotel[]> DaiElencoHotel()
        {
            if (string.IsNullOrEmpty(BaseUrl))
            {
                throw new InvalidOperationException(
                    "BaseUrl non è stato impostato. Chiamare SetBaseUrl prima di utilizzare questo metodo."
                );
            }

            const int pageSize = 50;
            int skip = 0;
            List<Hotel> hotels = new List<Hotel>();
            bool hasMore = true;

            var httpClient = new HttpClient();
            XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
            XNamespace atom = "http://www.w3.org/2005/Atom"; // Namespace di Atom per l'elemento "entry"

            while (hasMore)
            {
                string pagedUrl;
                if (BaseUrl.Contains("$skip="))
                {
                    pagedUrl = System.Text.RegularExpressions.Regex.Replace(
                        BaseUrl,
                        @"\$skip=\d+",
                        $"$skip={skip}"
                    );
                }
                else
                {
                    pagedUrl = BaseUrl.Contains("?")
                        ? $"{BaseUrl}&$skip={skip}"
                        : $"{BaseUrl}?$skip={skip}";
                }

                var response = await httpClient.GetAsync(pagedUrl);
                string contents = await response.Content.ReadAsStringAsync();

                XDocument xml = XDocument.Parse(contents);

                var entries = xml.Descendants(atom + "entry").ToList();
                hasMore = entries.Count == pageSize;

                foreach (var entry in entries)
                {
                    var contentElement = entry.Element(atom + "content");
                    if (contentElement != null)
                    {
                        var properties = contentElement.Element(m + "properties");
                        if (properties != null)
                        {
                            hotels.Add(
                                new Hotel
                                {
                                    PartitaIva =
                                        properties.Element(d + "Ccpartita_iva_1140518421")?.Value
                                        ?? "Sconosciuto",
                                    Comune =
                                        properties.Element(d + "Cccomune_608711150")?.Value
                                        ?? "Sconosciuto",
                                    DenominazioneStruttura =
                                        properties.Element(d + "Ccdenominazione1291548260")?.Value
                                        ?? "Sconosciuto",
                                    Tipologia =
                                        properties.Element(d + "Cctipologia_alb1238256790")?.Value
                                        ?? "Sconosciuto",
                                    Categoria =
                                        properties.Element(d + "Cccategoria_709124048")?.Value
                                        ?? "Sconosciuto",
                                    Indirizzo =
                                        properties.Element(d + "Ccindirizzo_1322096747")?.Value
                                        ?? "Sconosciuto",
                                    Frazione =
                                        properties.Element(d + "Ccfrazione_182197691")?.Value
                                        ?? "Sconosciuto",
                                    CAP =
                                        properties.Element(d + "Cccap_3047567")?.Value
                                        ?? "Sconosciuto",
                                    Telefono =
                                        properties.Element(d + "Cctelefono_90134615")?.Value
                                        ?? "Sconosciuto",
                                    FAX =
                                        properties.Element(d + "Ccfax_3050458")?.Value
                                        ?? "Sconosciuto",
                                    IndirizzoPostaElettronica =
                                        properties.Element(d + "Ccindirizzo_pos1351423458")?.Value
                                        ?? "Sconosciuto",
                                    TipologiaServizio =
                                        properties.Element(d + "Cctipologia_serv481601159")?.Value
                                        ?? "Sconosciuto",
                                    Altitudine =
                                        properties.Element(d + "Ccaltitudine_1110206336")?.Value
                                        ?? "Sconosciuto",
                                    NumeroPostiletto =
                                        properties.Element(d + "Ccnumero_posti_1304069088")?.Value
                                        ?? "Sconosciuto",
                                }
                            );
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

                skip += pageSize;
            }


            return hotels.ToArray();
        }

        public static async Task<Hotel[]> RicercaHotelPerComune(string comune)
        {
           
            Hotel[] TuttiHotel = await DaiElencoHotel();
            return TuttiHotel.Where(s => s.Comune.ToLower().Contains(comune.ToLower())).ToArray();
        }

        public static async Task<Hotel> RicercaHotelPerPIVA(string pIva)
        {
           Hotel[] TuttiHotel = await DaiElencoHotel();
            return TuttiHotel.FirstOrDefault(s => s.PartitaIva.Equals(pIva));
        }

        public static async Task<String[]> ComuniDisponibili()
        {
            Hotel[] TuttiHotel = await DaiElencoHotel();
            return TuttiHotel.Select(h => h.Comune).Distinct().ToArray(); //necessario per menù a tendina
        }
    }
}
