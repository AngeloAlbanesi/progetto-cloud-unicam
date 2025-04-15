using System; // Aggiunto per TimeSpan
using System.Diagnostics;
using System.Threading.Tasks; // Aggiunto per Task e await
using FunzioniDatiHotell;
using FunzioniDatiHotell.Modelli;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory; // Aggiunto per IMemoryCache
using ProvinciaTrentoHotel.ViewModels;

namespace ProvinciaTrentoHotel.Controllers
{
    /// <summary>
    /// Controller principale per la gestione delle pagine Home e delle funzionalità di ricerca e visualizzazione hotel.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Restituisce la vista della pagina principale (Home).
        /// </summary>
        /// <returns>Un ActionResult che renderizza la vista Index.</returns>
        public ActionResult Index()
        {
            return View();
        }

        // Modificato per caching e async
        /// <summary>
        /// Mostra la vista con l'elenco completo degli hotel.
        /// Utilizza una cache in memoria (IMemoryCache) per ottimizzare i caricamenti successivi.
        /// </summary>
        /// <returns>
        /// Un Task&lt;IActionResult&gt; che rappresenta l'operazione asincrona.
        /// Il risultato è un ActionResult che renderizza la vista ElencoHotel con il modello HomeElencoHotelViewModel popolato.
        /// </returns>
        public async Task<IActionResult> ElencoHotel()
        {
            Hotel[] elencoHotel = await FunzioneInterrogazioneDati.DaiElencoHotel(); // Usa await

            HomeElencoHotelViewModel vm = new HomeElencoHotelViewModel
            {
                ElencoHotel = elencoHotel,
            };
            return View(vm);
        }

        [HttpPost]
        // Modificato per async
        /// <summary>
        /// Gestisce la richiesta POST per la ricerca di hotel in un comune specifico.
        /// </summary>
        /// <param name="InputComune">Il nome del comune inserito dall'utente nel form di ricerca.</param>
        /// <returns>
        /// Un Task&lt;IActionResult&gt; che rappresenta l'operazione asincrona.
        /// Il risultato è un ActionResult che renderizza la vista RicercaHotelComune con il modello HomeRicercaHotelComuneViewModel popolato con i risultati della ricerca e l'elenco dei comuni disponibili.
        /// </returns>
        public async Task<IActionResult> RicercaHotelComune(string InputComune)
        {
            HomeRicercaHotelComuneViewModel vmRicercaComune = new HomeRicercaHotelComuneViewModel();

            // Esegui le chiamate 
            Hotel[] ricercaHotel = await FunzioneInterrogazioneDati.RicercaHotelPerComune(
                InputComune
            ); // Usa await
            string[] comuniDisponibili = await FunzioneInterrogazioneDati.ComuniDisponibili(); // Usa await

            vmRicercaComune.RicercaHotel = ricercaHotel;
            vmRicercaComune.ComuniDisponibili = comuniDisponibili;

            return View(vmRicercaComune);
        }

        // Modificato per async
        /// <summary>
        /// Mostra la vista per la ricerca di hotel per comune, popolando l'elenco a discesa dei comuni disponibili.
        /// </summary>
        /// <returns>
        /// Un Task&lt;IActionResult&gt; che rappresenta l'operazione asincrona.
        /// Il risultato è un ActionResult che renderizza la vista RicercaHotelComune con il modello HomeRicercaHotelComuneViewModel (senza risultati di ricerca iniziali).
        /// </returns>
        public async Task<IActionResult> RicercaHotelComune()
        {
            HomeRicercaHotelComuneViewModel vmRicercaComune = new HomeRicercaHotelComuneViewModel();
            vmRicercaComune.ComuniDisponibili =
                await FunzioneInterrogazioneDati.ComuniDisponibili(); // Usa await
            return View(vmRicercaComune);
        }

        // Modificato per async
        /// <summary>
        /// Mostra la vista con i dettagli di un hotel specifico, identificato dalla sua Partita IVA.
        /// </summary>
        /// <param name="pIva">La Partita IVA dell'hotel di cui visualizzare i dettagli.</param>
        /// <returns>
        /// Un Task&lt;IActionResult&gt; che rappresenta l'operazione asincrona.
        /// Il risultato è un ActionResult che renderizza la vista DettagliHotel con il modello HomeDettagliHotelViewModel popolato.
        /// </returns>
        public async Task<IActionResult> DettagliHotel(string pIva)
        {
            HomeDettagliHotelViewModel vmDettagliHotel = new HomeDettagliHotelViewModel();
            Hotel hotelDettagliato = await FunzioneInterrogazioneDati.RicercaHotelPerPIVA(pIva); // Usa await
            vmDettagliHotel.hotelDettagliato = hotelDettagliato;
            return View(vmDettagliHotel);
        }
    }
}
