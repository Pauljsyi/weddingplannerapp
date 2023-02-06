#pragma warning disable CS8618;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WeddingPlanner.Models;
public class Rsvp
{
    [Key]
    public int RsvpId { get; set; }


    public int UserId { get; set; }

    public int WeddingId { get; set; }

    // Navigation Properties
    public User? User { get; set; }
    public Wedding? Wedding { get; set; }


}