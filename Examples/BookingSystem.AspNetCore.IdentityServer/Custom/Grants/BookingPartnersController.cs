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
    [Route("booking-partners")]
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
        public async Task<IActionResult> Index()
        {
            return View("Index", await BuildViewModel());
        }

        /// <summary>
        /// Show list of grants
        /// </summary>
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var content = await BuildBookingPartnerViewModel(id);
            if (content == null)
                return NotFound();

            return View("BookingPartnerEdit", content);
        }

        /// <summary>
        /// Show list of grants
        /// </summary>
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            return View("BookingPartnerCreate", await Task.FromResult(new BookingPartnerModel()));
        }

        /// <summary>
        /// Add a new booking partner
        /// </summary>
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] string email, [FromForm] string bookingPartnerName)
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

            await FakeBookingSystem.Database.AddBookingPartner(newBookingPartner);
            return Redirect($"/booking-partners/edit/{newBookingPartner.ClientId}");
        }

        /// <summary>
        /// Handle postback to revoke a client
        /// </summary>
        [HttpPost("manage-keys")]
        [ValidateAntiForgeryToken]
        public IActionResult ManageKeys([FromForm] string clientId)
        {
            return Redirect("/booking-partners");
        }

        /// <summary>
        /// Handle postback to revoke a client
        /// </summary>
        [HttpPost("restore")]
        [ValidateAntiForgeryToken]
        public IActionResult Restore([FromForm] string clientId)
        {
            return Redirect("/booking-partners");
        }

        /// <summary>
        /// Handle postback to revoke a client
        /// </summary>
        [HttpPost("revoke")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke([FromForm] string clientId)
        {
            await _interaction.RevokeUserConsentAsync(clientId);
            await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));

            return Redirect("/booking-partners");
        }

        /// <summary>
        /// Handle postback to suspend a client
        /// </summary>
        [HttpPost("suspend")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend([FromForm] string clientId)
        {
            var client = await _clients.FindClientByIdAsync(clientId);
            client.AllowedScopes.Remove("openactive-openbooking");
            await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));

            await FakeBookingSystem.Database.UpdateBookingPartnerScope(clientId, "openid profile openactive-ordersfeed", true);
            return Redirect("/booking-partners");
        }

        /// <summary>
        /// Handle postback to generate a registration key
        /// </summary>
        [HttpPost("regenerate-key")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateKey([FromForm] string clientId)
        {
            var bookingPartner = await FakeBookingSystem.Database.GetBookingPartner(clientId);
            await FakeBookingSystem.Database.SetBookingPartnerKey(clientId, KeyGenerator.GenerateInitialAccessToken(bookingPartner.Name));

            return Redirect($"/booking-partners/edit/{clientId}");
        }

        /// <summary>
        /// Handle postback to generate a registration key, and a new client secret
        /// </summary>
        [HttpPost("regenerate-all-keys")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateAllKeys([FromForm] string clientId)
        {
            var bookingPartner = await FakeBookingSystem.Database.GetBookingPartner(clientId);
            await FakeBookingSystem.Database.ResetBookingPartnerKey(clientId, KeyGenerator.GenerateInitialAccessToken(bookingPartner.Name));

            // TODO: Is this cached in memory, does it need updating??
            //var client = await _clients.FindClientByIdAsync(clientId);
            //client.ClientSecrets = new List<Secret>() { new Secret(clientSecret.Sha256()) };

            return Redirect($"/booking-partners/edit/{clientId}");
        }

        private static async Task<BookingPartnerModel> BuildBookingPartnerViewModel(string clientId)
        {
            // var client = await _clients.FindClientByIdAsync(clientId);
            var bookingPartner = await FakeBookingSystem.Database.GetBookingPartner(clientId);
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

        private static async Task<BookingPartnerViewModel> BuildViewModel()
        {
            var bookingPartners = await FakeBookingSystem.Database.GetBookingPartners();
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