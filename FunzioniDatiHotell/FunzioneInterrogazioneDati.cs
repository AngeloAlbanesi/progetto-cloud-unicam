using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using FunzioniDatiHotell.Modelli;

namespace FunzioniDatiHotell
{
    /// <summary>
    /// Classe che gestisce l'interrogazione e il recupero dei dati relativi agli hotel della Provincia di Trento.
    /// Fornisce funzionalità per ottenere l'elenco completo degli hotel, ricercare hotel per comune o partita IVA,
    /// e ottenere l'elenco dei comuni disponibili.
    /// </summary>
    public class FunzioneInterrogazioneDati
    {
        /// <summary>
        /// URL base per le richieste API.
        /// </summary>
        private static string BaseUrl;

        /// <summary>
        /// Array contenente tutti i comuni disponibili. Viene inizializzato alla prima richiesta.
        /// </summary>
        private static string[] TuttiComuni;

        /// <summary>
        /// Dimensione della pagina per le richieste paginate.
        /// </summary>
        private const int PAGE_SIZE = 50;

        /// <summary>
        /// Namespace XML utilizzati per il parsing delle risposte.
        /// </summary>
        private static readonly XNamespace D =
            "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace M =
            "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private static readonly XNamespace ATOM = "http://www.w3.org/2005/Atom";

        /// <summary>
        /// Client HTTP utilizzato per effettuare le richieste.
        /// </summary>
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Imposta l'URL base per le richieste API.
        /// </summary>
        /// <param name="url">L'URL base da utilizzare per le richieste.</param>
        public static void SetBaseUrl(string url)
        {
            BaseUrl = url;
        }

        /// <summary>
        /// Verifica che l'URL base sia stato impostato.
        /// </summary>
        /// <exception cref="InvalidOperationException">Lanciata quando l'URL base non è stato impostato.</exception>
        private static void VerifyBaseUrlIsSet()
        {
            if (string.IsNullOrEmpty(BaseUrl))
            {
                throw new InvalidOperationException(
                    "BaseUrl non è stato impostato. Chiamare SetBaseUrl prima di utilizzare questo metodo."
                );
            }
        }

        /// <summary>
        /// Recupera l'elenco completo di tutti gli hotel.
        /// </summary>
        /// <returns>Un array contenente tutti gli hotel disponibili.</returns>
        public static async Task<Hotel[]> DaiElencoHotel()
        {
            VerifyBaseUrlIsSet();
            return await FetchHotels();
        }

        /// <summary>
        /// Ricerca gli hotel in un determinato comune.
        /// </summary>
        /// <param name="comune">Il nome del comune in cui cercare gli hotel.</param>
        /// <returns>Un array contenente gli hotel trovati nel comune specificato.</returns>
        public static async Task<Hotel[]> RicercaHotelPerComune(string comune)
        {
            VerifyBaseUrlIsSet();
            // Gestione apostrofi per OData: raddoppia ogni apostrofo singolo
            string comuneODataSafe = comune.Replace("'", "''");
            string filter = $"Cccomune_608711150 eq '{comuneODataSafe}'";
            return await FetchHotels(filter);
        }

        /// <summary>
        /// Ricerca un hotel specifico tramite la sua partita IVA.
        /// </summary>
        /// <param name="pIva">La partita IVA dell'hotel da cercare.</param>
        /// <returns>L'hotel corrispondente alla partita IVA specificata, o null se non trovato.</returns>
        public static async Task<Hotel> RicercaHotelPerPIVA(string pIva)
        {
            VerifyBaseUrlIsSet();
            string filter = $"Ccpartita_iva_1140518421 eq '{pIva}'";
            Hotel[] hotels = await FetchHotels(filter);
            return hotels.FirstOrDefault();
        }

        /// <summary>
        /// Recupera l'elenco di tutti i comuni in cui sono presenti hotel.
        /// </summary>
        /// <returns>Un array contenente i nomi dei comuni disponibili.</returns>
        public static async Task<String[]> ComuniDisponibili()
        {
            if (TuttiComuni != null)
            {
                return TuttiComuni;
            }

            VerifyBaseUrlIsSet();
            HashSet<string> comuni = new HashSet<string>();
            int skip = 0;
            bool hasMore = true;

            while (hasMore)
            {
                string pagedUrl = BuildUrl(skip, null, "Cccomune_608711150");
                var response = await _httpClient.GetAsync(pagedUrl);
                string contents = await response.Content.ReadAsStringAsync();
                XDocument xml = XDocument.Parse(contents);

                var entries = xml.Descendants(ATOM + "entry").ToList();
                hasMore = entries.Count == PAGE_SIZE;

                foreach (var entry in entries)
                {
                    var contentElement = entry.Element(ATOM + "content");
                    if (contentElement != null)
                    {
                        var properties = contentElement.Element(M + "properties");
                        if (properties != null)
                        {
                            var comune = properties.Element(D + "Cccomune_608711150")?.Value;
                            if (!string.IsNullOrWhiteSpace(comune))
                            {
                                comuni.Add(comune.Trim());
                            }
                        }
                    }
                }

                skip += PAGE_SIZE;
            }

            TuttiComuni = comuni.ToArray();
            return TuttiComuni;
        }

        /// <summary>
        /// Recupera gli hotel dal servizio, con possibilità di applicare un filtro.
        /// </summary>
        /// <param name="filter">Il filtro da applicare alla ricerca (opzionale).</param>
        /// <returns>Un array contenente gli hotel che corrispondono ai criteri di ricerca.</returns>
        private static async Task<Hotel[]> FetchHotels(string filter = null)
        {
            List<Hotel> hotels = new List<Hotel>();
            int skip = 0;
            bool hasMore = true;

            while (hasMore)
            {
                string pagedUrl = BuildUrl(skip, filter);
                var response = await _httpClient.GetAsync(pagedUrl);
                string contents = await response.Content.ReadAsStringAsync();

                XDocument xml = XDocument.Parse(contents);
                var entries = xml.Descendants(ATOM + "entry").ToList();
                hasMore = entries.Count == PAGE_SIZE;

                foreach (var entry in entries)
                {
                    var hotel = ExtractHotelFromXmlEntry(entry);
                    if (hotel != null)
                    {
                        hotels.Add(hotel);
                    }
                }

                skip += PAGE_SIZE;
            }

            return hotels.ToArray();
        }

        /// <summary>
        /// Costruisce l'URL per la richiesta API, includendo i parametri di paginazione e filtro.
        /// </summary>
        /// <param name="skip">Il numero di risultati da saltare (per la paginazione).</param>
        /// <param name="filter">Il filtro da applicare alla ricerca (opzionale).</param>
        /// <param name="select">I campi da selezionare nella risposta (opzionale).</param>
        /// <returns>L'URL completo per la richiesta API.</returns>
        private static string BuildUrl(int skip, string filter = null, string select = null)
        {
            string url = BaseUrl;
            bool hasQueryParams = url.Contains("?");

            // Handle $skip parameter
            if (url.Contains("$skip="))
            {
                url = System.Text.RegularExpressions.Regex.Replace(
                    url,
                    @"\$skip=\d+",
                    $"$skip={skip}"
                );
            }
            else
            {
                url += hasQueryParams ? $"&$skip={skip}" : $"?$skip={skip}";
                hasQueryParams = true;
            }

            // Add filter if specified
            if (!string.IsNullOrEmpty(filter))
            {
                // Codifica URL del filtro per evitare errori con caratteri speciali
                string encodedFilter = Uri.EscapeDataString(filter);
                url += hasQueryParams ? $"&$filter={encodedFilter}" : $"?$filter={encodedFilter}";
                hasQueryParams = true;
            }

            // Add select if specified
            if (!string.IsNullOrEmpty(select) && !url.Contains("$select="))
            {
                url += hasQueryParams ? $"&$select={select}" : $"?$select={select}";
            }

            return url;
        }

        /// <summary>
        /// Estrae le informazioni dell'hotel da un elemento XML.
        /// </summary>
        /// <param name="entry">L'elemento XML contenente i dati dell'hotel.</param>
        /// <returns>Un oggetto Hotel popolato con i dati estratti, o null se i dati non sono validi.</returns>
        private static Hotel ExtractHotelFromXmlEntry(XElement entry)
        {
            var contentElement = entry.Element(ATOM + "content");
            if (contentElement == null)
            {
                return null;
            }

            var properties = contentElement.Element(M + "properties");
            if (properties == null)
            {
                return null;
            }

            return new Hotel
            {
                PartitaIva =
                    properties.Element(D + "Ccpartita_iva_1140518421")?.Value ?? "Sconosciuto",
                Comune = properties.Element(D + "Cccomune_608711150")?.Value ?? "Sconosciuto",
                DenominazioneStruttura =
                    properties.Element(D + "Ccdenominazione1291548260")?.Value ?? "Sconosciuto",
                Tipologia =
                    properties.Element(D + "Cctipologia_alb1238256790")?.Value ?? "Sconosciuto",
                Categoria = properties.Element(D + "Cccategoria_709124048")?.Value ?? "Sconosciuto",
                Indirizzo =
                    properties.Element(D + "Ccindirizzo_1322096747")?.Value ?? "Sconosciuto",
                Frazione = properties.Element(D + "Ccfrazione_182197691")?.Value ?? "Sconosciuto",
                CAP = properties.Element(D + "Cccap_3047567")?.Value ?? "Sconosciuto",
                Telefono = properties.Element(D + "Cctelefono_90134615")?.Value ?? "Sconosciuto",
                FAX = properties.Element(D + "Ccfax_3050458")?.Value ?? "Sconosciuto",
                IndirizzoPostaElettronica =
                    properties.Element(D + "Ccindirizzo_pos1351423458")?.Value ?? "Sconosciuto",
                TipologiaServizio =
                    properties.Element(D + "Cctipologia_serv481601159")?.Value ?? "Sconosciuto",
                Altitudine =
                    properties.Element(D + "Ccaltitudine_1110206336")?.Value ?? "Sconosciuto",
                NumeroPostiletto =
                    properties.Element(D + "Ccnumero_posti_1304069088")?.Value ?? "Sconosciuto",
            };
        }
    }
}
