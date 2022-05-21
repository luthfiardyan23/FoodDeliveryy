using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using UserService.Models;

namespace UserService.GraphQL
{
    public class Query
    {
        public IQueryable<Food> GetProducts([Service] FoodDeliveryContext context) =>
            context.Foods;

        [Authorize] // dapat diakses kalau sudah login
        public IQueryable<UserData> GetUsers([Service] FoodDeliveryContext context) =>
            context.Users.Select(p => new UserData()
            {
                Id = p.Id,
                FullName = p.FullName,
                Email = p.Email,
                Username = p.Username
            });

        [Authorize] 
        public IQueryable<Profile> GetProfiles([Service] FoodDeliveryContext context, ClaimsPrincipal claimsPrincipal)
        {
            var userName = claimsPrincipal.Identity.Name;

            // check admin role ?
            var adminRole = claimsPrincipal.Claims.Where(o => o.Type == ClaimTypes.Role && o.Value == "Admin").FirstOrDefault();
            var user = context.Users.Where(o => o.Username == userName).FirstOrDefault();
            if (user != null)
            {
                if (adminRole!=null)                    
                {                    
                    return context.Profiles;
                }
                var profiles = context.Profiles.Where(o => o.UserId == user.Id);                
                return profiles.AsQueryable();
            }


            return new List<Profile>().AsQueryable();
        }

        [Authorize]
        public IQueryable<Profile> GetProfilesbyToken([Service] FoodDeliveryContext context, ClaimsPrincipal claimsPrincipal)
        {
            var userName = claimsPrincipal.Identity.Name;
            var user = context.Users.Where(o => o.Username == userName).FirstOrDefault();
            if (user != null)
            {
                var profiles = context.Profiles.Where(o => o.UserId == user.Id);
                return profiles.AsQueryable();
            }
            return new List<Profile>().AsQueryable();
        }

        [Authorize(Roles = new[] { "Manager" })]
        public IQueryable<User> GetCouriers([Service] FoodDeliveryContext context)
        {
            var roleKurir = context.Roles.Where(k => k.Name == "Manager").FirstOrDefault();
            var kurirs = context.Users.Where(k => k.UserRoles.Any(o => o.RoleId == roleKurir.Id));
            return kurirs.AsQueryable();
        }

        [Authorize(Roles = new[] { "Manager" })]
        public IQueryable<Courier> GetCourierProfiles([Service] FoodDeliveryContext context) =>
            context.Couriers.Select(p => new Courier()
            {
                Id = p.Id,
                CourierName = p.CourierName,
                PhoneNumber = p.PhoneNumber,
                UserId = p.UserId,
                Availibility = p.Availibility
            });

    }
}
