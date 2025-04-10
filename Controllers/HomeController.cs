/*
 *   Copyright (c) 2025
 *   All rights reserved.
 */
using System.Diagnostics;
using FunzioniDatiHotell;
using FunzioniDatiHotell.Modelli;
using Microsoft.AspNetCore.Mvc;
using ProvinciaTrentoHotel.Models; // Aggiunto per ErrorViewModel
using Microsoft.Extensions.Logging; // Aggiunto per logging
using ProvinciaTrentoHotel.ViewModels;
using System.Threading.Tasks; // Aggiunto per Task

namespace ProvinciaTrentoHotel.Controllers
{
    public class HomeController : Controller
    {
        private readonly FunzioneInterrogazioneDati _funzioniDati;
        private readonly ILogger<HomeController> _logger;

        // Costruttore per Dependency Injection
        public HomeController(FunzioneInterrogazioneDati funzioniDati, ILogger<HomeController> logger)
        {
            _funzioniDati = funzioniDati;
            _logger = logger;
        }
        // Rimuovo il costruttore statico, l'inizializzazione ora è gestita dalla cache on-demand

        public ActionResult Index()
        {
            return View();
        }

        // Modifico per usare async/await e l'istanza iniettata
        public async Task<IActionResult> ElencoHotel()
        {
            _logger.LogInformation("Accesso alla pagina ElencoHotel.");
            HomeElencoHotelViewModel vm = new HomeElencoHotelViewModel();
            try
            {
                vm.ElencoHotel = await _funzioniDati.DaiElencoHotel();
                _logger.LogInformation("Recuperati {Count} hotel per ElencoHotel.", vm.ElencoHotel.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dell'elenco hotel.");
                // Gestire l'errore, magari mostrando un messaggio all'utente
                vm.ElencoHotel = Array.Empty<Hotel>(); // Array vuoto in caso di errore
                // Potresti voler reindirizzare a una pagina di errore o aggiungere un messaggio al modello
                // TempData["ErrorMessage"] = "Si è verificato un errore nel caricamento degli hotel.";
            }
            return View(vm);
        }

        [HttpPost]
        // Modifico per usare async/await e l'istanza iniettata
        public async Task<IActionResult> RicercaHotelComune(string InputComune)
        {
             _logger.LogInformation("Esecuzione ricerca hotel per comune: {Comune}", InputComune);
            HomeRicercaHotelComuneViewModel vmRicercaComune = new HomeRicercaHotelComuneViewModel();
            try
            {
                // Eseguo le chiamate in parallelo dove possibile
                var ricercaTask = _funzioniDati.RicercaHotelPerComune(InputComune);
                var comuniTask = _funzioniDati.ComuniDisponibili();

                await Task.WhenAll(ricercaTask, comuniTask);

                vmRicercaComune.RicercaHotel = await ricercaTask;
                vmRicercaComune.ComuniDisponibili = await comuniTask;
                 _logger.LogInformation("Ricerca per {Comune} ha prodotto {Count} risultati. Comuni disponibili: {ComuniCount}", InputComune, vmRicercaComune.RicercaHotel.Length, vmRicercaComune.ComuniDisponibili.Length);
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Errore durante la ricerca hotel per comune: {Comune}", InputComune);
                vmRicercaComune.RicercaHotel = Array.Empty<Hotel>();
                vmRicercaComune.ComuniDisponibili = Array.Empty<string>();
                // TempData["ErrorMessage"] = "Si è verificato un errore nella ricerca.";
            }
            return View(vmRicercaComune);
        }

        // Modifico per usare async/await e l'istanza iniettata (versione GET)
        public async Task<IActionResult> RicercaHotelComune()
        {
             _logger.LogInformation("Accesso alla pagina RicercaHotelComune (GET).");
            HomeRicercaHotelComuneViewModel vmRicercaComune = new HomeRicercaHotelComuneViewModel();
            try
            {
                vmRicercaComune.ComuniDisponibili = await _funzioniDati.ComuniDisponibili();
                 _logger.LogInformation("Recuperati {ComuniCount} comuni disponibili per il menu.", vmRicercaComune.ComuniDisponibili.Length);
                vmRicercaComune.RicercaHotel = Array.Empty<Hotel>(); // Inizializza vuoto per la vista GET
            }
             catch (Exception ex)
            {
                 _logger.LogError(ex, "Errore durante il recupero dei comuni disponibili per RicercaHotelComune (GET).");
                vmRicercaComune.ComuniDisponibili = Array.Empty<string>();
                // TempData["ErrorMessage"] = "Si è verificato un errore nel caricamento dei comuni.";
            }
            return View(vmRicercaComune);
        }

        // Modifico per usare async/await e l'istanza iniettata
        public async Task<IActionResult> DettagliHotel(string pIva)
        {
             _logger.LogInformation("Accesso alla pagina DettagliHotel per PIVA: {PIVA}", pIva);
            HomeDettagliHotelViewModel vmDettagliHotel = new HomeDettagliHotelViewModel();
             if (string.IsNullOrWhiteSpace(pIva))
            {
                _logger.LogWarning("Tentativo di accesso a DettagliHotel senza PIVA.");
                return NotFound(); // O BadRequest()
            }
            try
            {
                vmDettagliHotel.hotelDettagliato = await _funzioniDati.RicercaHotelPerPIVA(pIva);
                if (vmDettagliHotel.hotelDettagliato == null)
                {
                     _logger.LogWarning("Hotel non trovato per PIVA: {PIVA}", pIva);
                    return NotFound(); // Hotel non trovato
                }
                 _logger.LogInformation("Dettagli hotel trovati per PIVA: {PIVA}", pIva);
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Errore durante il recupero dei dettagli hotel per PIVA: {PIVA}", pIva);
                // TempData["ErrorMessage"] = "Si è verificato un errore nel caricamento dei dettagli dell'hotel.";
                // Potresti voler reindirizzare a una pagina di errore generica
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
            return View(vmDettagliHotel);
        }
    }
}
