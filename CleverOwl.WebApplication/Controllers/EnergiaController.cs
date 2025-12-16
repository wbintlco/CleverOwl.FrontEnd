using System;

public class EnergiaController
{
    public async Task<IActionResult> RedirectToEnergia(string userId) { 
        
        string redirectUrl = await YourClass.EenergiaSignIn(userId); 
        return Json(new { url = redirectUrl }); 
    
    }
}
