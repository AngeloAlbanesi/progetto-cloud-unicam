using System.Diagnostics;
using System.Web;
using System.Xml.Linq;
using FunzioniDatiHotell.Modelli;

namespace FunzioniDatiHotell
{
    /// <summary>
    /// Classe statica per interrogare il servizio OData della Provincia di Trento
    /// e recuperare informazioni sugli hotel e i comuni disponibili.
    /// </summary>
    public class FunzioneInterrogazioneDati
    {
        private static string BaseUrl;
        private static string[] TuttiComuni;
        private const int PAGE_SIZE = 520;

        private static readonly XNamespace D =
            "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace M =
            "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private static readonly XNamespace ATOM = "http://www.w3.org/2005/Atom";
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Imposta l'URL base per le richieste al servizio OData.
        /// </summary>
        /// <param name="url">L'URL base del servizio OData.</param>
        public static void SetBaseUrl(string url)
        {
            BaseUrl = url;
        }

        /// <summary>
        /// Verifica che la variabile BaseUrl sia stata impostata.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Lanciata se BaseUrl non è stato impostato.
        /// </exception>
        private static void VerificaSetBaseUrl()
        {
            if (string.IsNullOrEmpty(BaseUrl))
            {
                throw new InvalidOperationException(
                    "BaseUrl non è stato impostato. Chiamare SetBaseUrl prima di utilizzare questo metodo."
                );
            }
        }

        /// <summary>
        /// Recupera l'elenco completo di tutti gli hotel disponibili.
        /// </summary>
        /// <returns>
        /// Un task che rappresenta l'operazione asincrona. Il risultato del task è un array di oggetti Hotel.
        /// </returns>
        public static async Task<Hotel[]> DaiElencoHotel()
        {
            VerificaSetBaseUrl();
            return await FetchHotels();
        }

        /// <summary>
        /// Ricerca e recupera gli hotel situati nel comune specificato.
        /// </summary>
        /// <param name="comune">Il nome del comune per cui cercare gli hotel.</param>
        /// <returns>
        /// Un task che rappresenta l'operazione asincrona. Il risultato del task è un array di oggetti Hotel trovati nel comune.
        /// </returns>
        public static async Task<Hotel[]> RicercaHotelPerComune(string comune)
        {
            VerificaSetBaseUrl();
            string comuneODataSafe = comune.Replace("'", "''"); // aggiunto perchè alcuni comuni contengono apostrofi
            string filter = $"Cccomune_608711150 eq '{comuneODataSafe}'";
            return await FetchHotels(filter);
        }

        /// <summary>
        /// Ricerca e recupera un hotel specifico tramite la sua Partita IVA.
        /// </summary>
        /// <param name="pIva">La Partita IVA dell'hotel da cercare.</param>
        /// <returns>
        /// Un task che rappresenta l'operazione asincrona. Il risultato del task è l'oggetto Hotel corrispondente o null se non trovato.
        /// </returns>
        public static async Task<Hotel> RicercaHotelPerPIVA(string pIva)
        {
            VerificaSetBaseUrl();
            string filter = $"Ccpartita_iva_1140518421 eq '{pIva}'";
            Hotel[] hotels = await FetchHotels(filter);
            return hotels.FirstOrDefault();
        }

        /// <summary>
        /// Recupera gli hotel dal servizio OData, gestendo il paging in modo parallelo.
        /// </summary>
        /// <param name="filter">Filtro OData opzionale per la ricerca (es. per comune o partita IVA).</param>
        /// <returns>
        /// Un task che rappresenta l'operazione asincrona. Il risultato del task è un array di oggetti Hotel.
        /// </returns>
        /// <exception cref="Exception">Segnala eventuali errori.</exception>
        private static async Task<Hotel[]> FetchHotels(string filter = null)
        {
            VerificaSetBaseUrl();
            // Cronometro per misurare il tempo di esecuzione delle richieste, UTILIZZATO per il debug
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Inizia recuperando la prima pagina per vedere se ci sono risultati
                string firstPageUrl = BuildUrl(
                    skip: 0,
                    filter: filter,
                    select: null,
                    top: PAGE_SIZE,
                    includeCount: false
                );

                Console.WriteLine($"[DEBUG] Richiesta prima pagina: {firstPageUrl}"); //UTILIZZATO PER IL DEBUG, url della prima pagina

                var response = await _httpClient.GetAsync(firstPageUrl);
                Console.WriteLine($"[DEBUG] Status code risposta: {response.StatusCode}");

                response.EnsureSuccessStatusCode();
                string contents = await response.Content.ReadAsStringAsync();
               

                XDocument xml = XDocument.Parse(contents);
                var entries = xml.Descendants(ATOM + "entry").ToList();
                Console.WriteLine(
                    $"[DEBUG] Numero entry trovate nella prima pagina: {entries.Count}"  //UTILIZZATO PER IL DEBUG, restituzione del numero di entry della pagina
                );

                if (entries.Count == 0)
                {
                    stopwatch.Stop();
                    Console.WriteLine(
                        $"FetchHotelsParallel completato in {stopwatch.ElapsedMilliseconds}ms (0 risultati)." //UTILIZZATO PER IL DEBUG, calcolo del tempo di risposta
                    );
                    return Array.Empty<Hotel>();
                }

                // Se abbiamo trovato risultati, recuperiamo tutte le pagine necessarie
                var allHotels = new List<Hotel>();
                int currentPage = 0;
                bool hasMore = true;

                while (hasMore)
                {
                    int skip = currentPage * PAGE_SIZE;
                    string pageUrl = BuildUrl(
                        skip: skip,
                        filter: filter,
                        select: null,
                        top: PAGE_SIZE,
                        includeCount: false
                    );

                    Console.WriteLine($"[DEBUG] Recupero pagina {currentPage + 1}, skip={skip}");   //UTILIZZATO PER IL DEBUG, restituisce url pagine successive

                    if (currentPage > 0) // Se non è la prima pagina, recupera i dati
                    {
                        response = await _httpClient.GetAsync(pageUrl);
                        response.EnsureSuccessStatusCode();
                        contents = await response.Content.ReadAsStringAsync();
                        xml = XDocument.Parse(contents);
                        entries = xml.Descendants(ATOM + "entry").ToList();
                    }

                    var pageHotels = entries
                        .Select(entry => ParseHotelDaXmlEntry(entry))
                        .Where(hotel => hotel != null)
                        .ToList();

                    allHotels.AddRange(pageHotels);
                    Console.WriteLine(
                        $"[DEBUG] Aggiunti {pageHotels.Count} hotel dalla pagina {currentPage + 1}" //UTILIZZATO PER IL DEBUG, restituisce il numero di hotel aggiunti
                    );

                    // Se abbiamo ricevuto meno hotel di PAGE_SIZE, abbiamo raggiunto l'ultima pagina
                    hasMore = entries.Count == PAGE_SIZE;
                    currentPage++;
                }

                stopwatch.Stop();
                Console.WriteLine(
                    $"FetchHotelsParallel completato in {stopwatch.ElapsedMilliseconds}ms ({allHotels.Count} risultati)." //UTILIZZATO PER IL DEBUG, calcolo del tempo di risposta
                );
                return allHotels.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Errore durante il recupero degli hotel: {ex.Message}"); //UTILIZZATO PER IL DEBUG, restituisce l'errore
                throw;
            }
        }

        /// <summary>
        /// Recupera l'elenco univoco e ordinato dei comuni in cui è presente almeno un hotel.
        /// </summary>
        /// <returns>
        /// Un task che rappresenta l'operazione asincrona. Il risultato del task è un array di stringhe contenente i nomi dei comuni.
        /// </returns>
        public static async Task<String[]> ComuniDisponibili()
        {

            //se l'array TuttiComuni è già stato popolato, restituiscilo
            if (TuttiComuni != null)
            {
                return TuttiComuni;
            }

            VerificaSetBaseUrl();
            //Utilizziamo HashSet per evitare duplicati
            HashSet<string> comuni = new HashSet<string>();
            int skip = 0;
            bool hasMore = true;

            while (hasMore)
            {
                string pagedUrl = BuildUrl(skip, null, "Cccomune_608711150", PAGE_SIZE);
                try
                {
                    var response = await _httpClient.GetAsync(pagedUrl);
                    response.EnsureSuccessStatusCode();
                    string contents = await response.Content.ReadAsStringAsync();
                    XDocument xml = XDocument.Parse(contents);

                    var entries = xml.Descendants(ATOM + "entry").ToList();
                    var nextLink = xml.Descendants(ATOM + "link")
                        .FirstOrDefault(link => link.Attribute("rel")?.Value == "next");
                    hasMore = nextLink != null || entries.Count == PAGE_SIZE;

                    foreach (var entry in entries)
                    {
                        var contentElement = entry.Element(ATOM + "content");
                        if (contentElement != null)
                        {
                            var properties = contentElement.Element(M + "properties");
                            if (properties != null)
                            {
                                var comuneValue = properties
                                    .Element(D + "Cccomune_608711150")
                                    ?.Value;
                                if (!string.IsNullOrWhiteSpace(comuneValue))
                                {
                                    comuni.Add(comuneValue.Trim());
                                }
                            }
                        }
                    }
                    if (hasMore)
                    {
                        skip += PAGE_SIZE;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore durante il recupero dei comuni: {ex.Message}");
                    throw;
                }
            }

            TuttiComuni = comuni.OrderBy(c => c).ToArray();
            return TuttiComuni;
        }

        /// <summary>
        /// Costruisce l'URL per la richiesta OData, includendo parametri di paging, filtro, selezione e conteggio.
        /// </summary>
        /// <param name="skip">Numero di record da saltare (per il paging).</param>
        /// <param name="filter">Filtro OData opzionale.</param>
        /// <param name="select">Campi da selezionare (opzionale).</param>
        /// <param name="top">Numero massimo di record da recuperare (opzionale).</param>
        /// <param name="includeCount">Se true, include il conteggio totale dei record.</param>
        /// <returns>L'URL completo per la richiesta OData.</returns>
        private static string BuildUrl(
            int skip,
            string filter = null,
            string select = null,
            int? top = null,
            bool includeCount = false
        )
        {
            var uriBuilder = new UriBuilder(BaseUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["$skip"] = skip.ToString();

            int? effectiveTop = top;
            bool setPageSize = false;
            if (!effectiveTop.HasValue && !includeCount)
            {
                setPageSize = true;
            }
            if (setPageSize)
            {
                effectiveTop = PAGE_SIZE;
            }

            if (effectiveTop.HasValue)
            {
                query["$top"] = effectiveTop.Value.ToString();
            }

            if (!string.IsNullOrEmpty(filter))
            {
                query["$filter"] = filter;
            }

            if (!string.IsNullOrEmpty(select))
            {
                query["$select"] = select;
            }

            if (includeCount)
            {
                query["$count"] = "true";
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.ToString();
        }

        /// <summary>
        /// Estrae un oggetto Hotel da una entry XML OData.
        /// </summary>
        /// <param name="entry">L'elemento XML che rappresenta una entry di hotel.</param>
        /// <returns>L'oggetto Hotel estratto, oppure null se il parsing fallisce.</returns>
        private static Hotel ParseHotelDaXmlEntry(XElement entry)
        {
            var contentElement = entry?.Element(ATOM + "content");
            if (contentElement == null)
                return null;

            var properties = contentElement.Element(M + "properties");
            if (properties == null)
                return null;

            string GetValueOrDefault(XName elementName, string defaultValue = "Sconosciuto") =>
                properties.Element(elementName)?.Value ?? defaultValue;

            try
            {
                return new Hotel
                {
                    PartitaIva = GetValueOrDefault(D + "Ccpartita_iva_1140518421"),
                    Comune = GetValueOrDefault(D + "Cccomune_608711150"),
                    DenominazioneStruttura = GetValueOrDefault(D + "Ccdenominazione1291548260"),
                    Tipologia = GetValueOrDefault(D + "Cctipologia_alb1238256790"),
                    Categoria = GetValueOrDefault(D + "Cccategoria_709124048"),
                    Indirizzo = GetValueOrDefault(D + "Ccindirizzo_1322096747"),
                    Frazione = GetValueOrDefault(D + "Ccfrazione_182197691"),
                    CAP = GetValueOrDefault(D + "Cccap_3047567"),
                    Telefono = GetValueOrDefault(D + "Cctelefono_90134615"),
                    FAX = GetValueOrDefault(D + "Ccfax_3050458"),
                    IndirizzoPostaElettronica = GetValueOrDefault(D + "Ccindirizzo_pos1351423458"),
                    TipologiaServizio = GetValueOrDefault(D + "Cctipologia_serv481601159"),
                    Altitudine = GetValueOrDefault(D + "Ccaltitudine_1110206336"),
                    NumeroPostiletto = GetValueOrDefault(D + "Ccnumero_posti_1304069088"),
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Errore durante il parsing dell'entry XML per un hotel: {ex.Message}"
                );
                return null;
            }
        }
    }
}
