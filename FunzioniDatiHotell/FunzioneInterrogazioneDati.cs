/*
 *   Copyright (c) 2025 Angelo Albanesi
 *   All rights reserved.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using FunzioniDatiHotell.Modelli;
using Microsoft.Extensions.Caching.Memory; // Aggiunto per IMemoryCache
using Microsoft.Extensions.Configuration; // Aggiunto per IConfiguration e GetValue
using Microsoft.Extensions.Logging; // Aggiunto per logging (opzionale ma utile)

namespace FunzioniDatiHotell
{
    // Rimuovo 'static' dalla classe
    public class FunzioneInterrogazioneDati
    {
        // Rendo _httpClient non statico o uso IHttpClientFactory (preferibile)
        // Per semplicità ora lo lascio statico, ma andrebbe migliorato
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<FunzioneInterrogazioneDati> _logger;
        private readonly string _baseUrl; // Rende BaseUrl specifico dell'istanza

        // Chiavi per la cache
        private const string CacheKeyElencoHotel = "ElencoHotelCompleto";
        private const string CacheKeyComuni = "ElencoComuniDisponibili";

        // Costruttore per Dependency Injection
        public FunzioneInterrogazioneDati(
            IMemoryCache memoryCache,
            IConfiguration configuration,
            ILogger<FunzioneInterrogazioneDati> logger
        )
        {
            _memoryCache = memoryCache;
            _logger = logger;
            // Leggo BaseUrl dalla configurazione all'interno del costruttore
            _baseUrl = configuration.GetValue<string>("DatiOpen:HotelApiBaseUri");
            if (string.IsNullOrEmpty(_baseUrl))
            {
                _logger.LogError(
                    "BaseUrl non è stato trovato nella configurazione 'DatiOpen:HotelApiBaseUri'."
                );
                throw new InvalidOperationException(
                    "BaseUrl non è stato trovato nella configurazione 'DatiOpen:HotelApiBaseUri'."
                );
            }
        }

        // Metodo principale per ottenere l'elenco hotel, ora usa IMemoryCache
        public async Task<Hotel[]> DaiElencoHotel()
        {
            // Provo a ottenere i dati dalla cache
            return await _memoryCache.GetOrCreateAsync(
                CacheKeyElencoHotel,
                async entry =>
                {
                    _logger.LogInformation(
                        "Cache miss per {CacheKey}. Recupero dati hotel dall'API.",
                        CacheKeyElencoHotel
                    );
                    // Imposto opzioni cache (es. scadenza)
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // Cache per 1 ora

                    // Logica di recupero dati migliorata
                    const int pageSize = 100; // Dimensione pagina
                    List<Hotel> hotels = new List<Hotel>();
                    int totalEntries = 0;
                    int skip = 0;
                    bool firstPage = true;

                    XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
                    XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
                    XNamespace atom = "http://www.w3.org/2005/Atom";

                    do
                    {
                        _logger.LogDebug(
                            "Recupero pagina hotel: skip={Skip}, pageSize={PageSize}",
                            skip,
                            pageSize
                        );
                        // Chiamo il metodo helper (ora non statico)
                        var pageResult = await FetchHotelsPage(skip, pageSize, d, m, atom);

                        if (firstPage)
                        {
                            // Tento di estrarre il conteggio totale dalla prima pagina
                            totalEntries = pageResult.TotalCount; // Assumiamo che FetchHotelsPage restituisca anche il totale
                            _logger.LogInformation(
                                "Numero totale di hotel rilevato: {TotalCount}",
                                totalEntries
                            );
                            firstPage = false;
                        }

                        hotels.AddRange(pageResult.Hotels);
                        skip += pageResult.Hotels.Count; // Incremento skip in base a quanti ne ho effettivamente ricevuti

                        // Continuo finché non ho recuperato tutti gli hotel (se il totale è noto)
                        // o finché la pagina non è vuota (se il totale non è noto o per sicurezza)
                        if (totalEntries > 0)
                        {
                            if (hotels.Count >= totalEntries)
                                break;
                        }
                        else // Se non conosco il totale, mi fermo quando una pagina ha meno elementi del richiesto o è vuota
                        {
                            if (pageResult.Hotels.Count < pageSize)
                                break;
                        }
                    } while (true); // Il ciclo si interrompe con 'break'

                    _logger.LogInformation(
                        "Recuperati {HotelCount} hotel dall'API. Aggiorno la cache.",
                        hotels.Count
                    );
                    return hotels.ToArray();
                }
            );
        }

        // Metodo helper per recuperare una singola pagina, ora non statico
        // Modificato per restituire anche il conteggio totale se disponibile
        private async Task<(List<Hotel> Hotels, int TotalCount)> FetchHotelsPage(
            int skip,
            int pageSize,
            XNamespace d,
            XNamespace m,
            XNamespace atom
        )
        {
            // Aggiungo $top per limitare i risultati per pagina e $count=true per ottenere il totale (standard OData)
            string pagedUrl = _baseUrl.Contains("?")
                ? $"{_baseUrl}&$skip={skip}&$top={pageSize}&$count=true"
                : $"{_baseUrl}?$skip={skip}&$top={pageSize}&$count=true";

            _logger.LogDebug("Chiamata API a: {Url}", pagedUrl);
            var response = await _httpClient.GetAsync(pagedUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Errore nella chiamata API a {Url}. Status Code: {StatusCode}",
                    pagedUrl,
                    response.StatusCode
                );
                response.EnsureSuccessStatusCode(); // Lancia eccezione se fallisce
            }

            string contents = await response.Content.ReadAsStringAsync();
            XDocument xml = XDocument.Parse(contents);
            var hotels = new List<Hotel>();
            int totalCount = -1; // Default a -1 se non trovato

            // Cerco il conteggio totale (OData V2/V3 lo mette nel feed)
            var countElement = xml.Root?.Element(m + "count");
            if (countElement != null && int.TryParse(countElement.Value, out int parsedCount))
            {
                totalCount = parsedCount;
            }

            var entries = xml.Descendants(atom + "entry");
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
                                    properties.Element(d + "Cccap_3047567")?.Value ?? "Sconosciuto",
                                Telefono =
                                    properties.Element(d + "Cctelefono_90134615")?.Value
                                    ?? "Sconosciuto",
                                FAX =
                                    properties.Element(d + "Ccfax_3050458")?.Value ?? "Sconosciuto",
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
                }
            }

            _logger.LogDebug(
                "Pagina recuperata con {HotelCount} hotel. Conteggio totale rilevato: {TotalCount}",
                hotels.Count,
                totalCount
            );
            return (hotels, totalCount);
        }

        // Metodo non statico, usa DaiElencoHotel (che usa la cache)
        public async Task<Hotel[]> RicercaHotelPerComune(string comune)
        {
            if (string.IsNullOrWhiteSpace(comune))
            {
                return Array.Empty<Hotel>(); // Restituisce array vuoto se il comune è nullo o vuoto
            }

            // Recupera tutti gli hotel (dalla cache se disponibile)
            Hotel[] tuttiHotel = await DaiElencoHotel();

            // Filtra gli hotel per il comune specificato (case-insensitive)
            var result = tuttiHotel
                .Where(h =>
                    h.Comune != null && h.Comune.Equals(comune, StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();

            _logger.LogDebug(
                "Ricerca per comune '{Comune}' ha prodotto {Count} risultati.",
                comune,
                result.Length
            );
            return result;
            // Non serve più cache specifica per comune
        }

        // Metodo non statico, usa DaiElencoHotel (che usa la cache)
        public async Task<Hotel> RicercaHotelPerPIVA(string pIva)
        {
            if (string.IsNullOrWhiteSpace(pIva))
            {
                return null;
            }
            Hotel[] tuttiHotel = await DaiElencoHotel();
            // Usa StringComparison per sicurezza
            return tuttiHotel.FirstOrDefault(h =>
                h.PartitaIva != null
                && h.PartitaIva.Equals(pIva, StringComparison.OrdinalIgnoreCase)
            );
        }

        // Metodo non statico, usa IMemoryCache per i comuni
        public async Task<string[]> ComuniDisponibili()
        {
            return await _memoryCache.GetOrCreateAsync(
                CacheKeyComuni,
                async entry =>
                {
                    _logger.LogInformation(
                        "Cache miss per {CacheKey}. Recupero elenco comuni.",
                        CacheKeyComuni
                    );
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // Cache per 1 ora

                    // Recupera tutti gli hotel (dalla cache se disponibile)
                    Hotel[] tuttiHotel = await DaiElencoHotel();

                    // Estrai i comuni distinti, ordina e gestisci null/vuoti
                    var result = tuttiHotel
                        .Select(h => h.Comune)
                        .Where(c => !string.IsNullOrWhiteSpace(c)) // Escludi comuni null o vuoti
                        .Distinct(StringComparer.OrdinalIgnoreCase) // Rendi distinct case-insensitive
                        .OrderBy(c => c) // Ordina alfabeticamente
                        .ToArray();

                    _logger.LogInformation(
                        "Recuperati {ComuniCount} comuni distinti. Aggiorno la cache.",
                        result.Length
                    );
                    return result;
                }
            );
        }

        // Rimuovo ClearCache statico, la cache ora è gestita da IMemoryCache
    }
}
