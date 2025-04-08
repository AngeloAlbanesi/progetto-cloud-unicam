using FunzioniDatiHotell.Modelli;
namespace ProvinciaTrentoHotel.ViewModels
{
    public class HomeRicercaHotelComuneViewModel
    {
        public Hotel[] RicercaHotel { get; set; }

        public String[] ComuniDisponibili { get; set; } //necessario per menu  a tendina
    }
}
