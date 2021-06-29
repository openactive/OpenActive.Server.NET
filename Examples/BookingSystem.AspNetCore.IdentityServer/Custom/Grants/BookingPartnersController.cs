// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using OpenActive.FakeDatabase.NET;
using System;
using System.Linq;
using IdentityServer;

namespace src
{
    /// <summary>
    /// This sample controller allows a user to revoke grants given to clients
    /// </summary>
    [SecurityHeaders]
    [Authorize]
    public class BookingPartnersController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clients;
        private readonly IEventService _events;

        public BookingPartnersController(IIdentityServerInteractionService interaction, IClientStore clients, IEventService events)
        {
            _interaction = interaction;
            _clients = clients;
            _events = events;
        }

        /// <summary>
        /// Show list of grants
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View("Index", BuildViewModel());
        }

        /// <summary>
        /// Show list of grants
        /// </summary>
        [HttpGet]
        public IActionResult Edit(string id)
        {
            var content = BuildBookingPartnerViewModel(id);
            if (content == null)
                return NotFound();

            return View("BookingPartnerEdit", content);
        }

        /// <summary>
        /// Show list of grants
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View("BookingPartnerCreate", await Task.FromResult(new BookingPartnerModel()));
        }

        /// <summary>
        /// Add a new booking partner
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBookingPartner(string email, string bookingPartnerName)
        {
            var newBookingPartner = new BookingPartnerTable
            {
                ClientId = Guid.NewGuid().ToString(),
                Name = bookingPartnerName,
                ClientSecret = null,
                Email = email,
                Registered = false,
                InitialAccessToken = KeyGenerator.GenerateInitialAccessToken(bookingPartnerName),
                InitialAccessTokenKeyValidUntil = DateTime.Now.AddDays(2),
                CreatedDate = DateTime.Now,
                BookingsSuspended = false
            };

            FakeBookingSystem.Database.AddBookingPartner(newBookingPartner);

            return View("BookingPartnerEdit", BuildBookingPartnerViewModel(newBookingPartner.ClientId));
        }

        /// <summary>
        /// Handle postback to revoke a client
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManageKeys(string clientId)
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Handle postback to revoke a client
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Restore(string clientId)
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Handle postback to revoke a client
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(string clientId)
        {
            await _interaction.RevokeUserConsentAsync(clientId);
            await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Handle postback to suspend a client
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(string clientId)
        {
            var client = await _clients.FindClientByIdAsync(clientId);
            client.AllowedScopes.Remove("openactive-openbooking");
            await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));

            FakeBookingSystem.Database.UpdateBookingPartnerScope(clientId, "openid profile openactive-ordersfeed", true);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Handle postback to generate a registration key
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegenerateKey(string clientId)
        {
            var bookingPartner = FakeBookingSystem.Database.GetBookingPartner(clientId);
            FakeBookingSystem.Database.SetBookingPartnerKey(clientId, KeyGenerator.GenerateInitialAccessToken(bookingPartner.Name));

            return View("BookingPartnerEdit", BuildBookingPartnerViewModel(clientId));
        }

        /// <summary>
        /// Handle postback to generate a registration key, and a new client secret
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegenerateAllKeys(string clientId)
        {
            var bookingPartner = FakeBookingSystem.Database.GetBookingPartner(clientId);
            FakeBookingSystem.Database.ResetBookingPartnerKey(clientId, KeyGenerator.GenerateInitialAccessToken(bookingPartner.Name));

            // TODO: Is this cached in memory, does it need updating??
            //var client = await _clients.FindClientByIdAsync(clientId);
            //client.ClientSecrets = new List<Secret>() { new Secret(clientSecret.Sha256()) };

            return View("BookingPartnerEdit", BuildBookingPartnerViewModel(clientId));
        }

        private static BookingPartnerModel BuildBookingPartnerViewModel(string clientId)
        {
            // var client = await _clients.FindClientByIdAsync(clientId);
            var bookingPartner = FakeBookingSystem.Database.GetBookingPartner(clientId);
            if (bookingPartner == null)
                return null;

            return new BookingPartnerModel
            {
                ClientId = bookingPartner.ClientId,
                ClientName = bookingPartner.Name,
                ClientLogoUrl = bookingPartner.ClientProperties?.LogoUri,
                ClientUrl = bookingPartner.ClientProperties?.ClientUri,
                BookingPartner = bookingPartner
            };
        }
        private static BookingPartnerViewModel BuildViewModel()
        {
            var bookingPartners = FakeBookingSystem.Database.GetBookingPartners();
            var list = bookingPartners.Select(bookingPartner => new BookingPartnerModel
            {
                ClientId = bookingPartner.ClientId,
                ClientName = bookingPartner.Name,
                ClientLogoUrl = bookingPartner.ClientProperties?.LogoUri,
                ClientUrl = bookingPartner.ClientProperties?.ClientUri,
                BookingPartner = bookingPartner
            }).ToList();

            return new BookingPartnerViewModel { BookingPartners = list };
        }
    }
}