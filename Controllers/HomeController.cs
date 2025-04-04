using System.Diagnostics;
using FunzioniDatiHotell;
using FunzioniDatiHotell.Modelli;
using Microsoft.AspNetCore.Mvc;
using ProvinciaTrentoHotel.ViewModels;

namespace ProvinciaTrentoHotel.Controllers
{
    //PROVA
    //prova paolo
    public class HomeController : Controller
    {
        public IActionResult ElencoHotel()
        {
            HomeElencoHotelViewModel vm = new HomeElencoHotelViewModel();
            Hotel[] ElencoHotel = FunzioneInterrogazioneDati.DaiElencoHotel().Result;
            vm.ElencoHotel = ElencoHotel;
            return View(vm);
        }

        [HttpPost]
        public IActionResult RicercaHotelComune (string InputComune)
        {
            HomeRicercaHotelComuneViewModel vmRicercaComune = new HomeRicercaHotelComuneViewModel();
            Hotel[] RicercaHotel = FunzioneInterrogazioneDati.RicercaHotelPerComune(InputComune).Result;
            vmRicercaComune.RicercaHotel=RicercaHotel;
            return View(vmRicercaComune);
        }

        public IActionResult RicercaHotelComune()
        {
            return View(new HomeRicercaHotelComuneViewModel());
        }

        public IActionResult DettaglioHotel(string pIva)
        {
            HotelDettagliHotelViewModel vmDettaglioHotel = new HotelDettagliHotelViewModel();
            Hotel hotelDettagliato = FunzioneInterrogazioneDati.RicercaHotelPerPIVA(pIva).Result;
            vmDettaglioHotel.hotelDettagliato = hotelDettagliato;
            return View(vmDettaglioHotel);
        }
    }
}
