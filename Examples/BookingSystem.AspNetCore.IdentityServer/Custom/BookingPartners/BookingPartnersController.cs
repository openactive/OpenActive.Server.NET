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
using IdentityServerHost.Quickstart.UI;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServer
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

        [HttpGet("seller-admin")]
        public async Task<IActionResult> SellerAdmin()
        {
            var sellerUserId = long.Parse(User.GetSubjectId());
            return View("SellerAdmin", await BookingPartnerViewModel.Build(sellerUserId));
        }

        [HttpGet("sys-admin")]
        public async Task<IActionResult> SysAdmin()
        {
            return View("SysAdmin", await BookingPartnerViewModel.Build());
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var content = await BuildBookingPartnerModel(id);
            if (content == null)
                return NotFound();

            return View("BookingPartnerEdit", content);
        }

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

            await BookingPartnerTable.Add(newBookingPartner);
            return Redirect($"/booking-partners/edit/{newBookingPartner.ClientId}");
        }

        /// <summary>
        /// Handle postback to remove a client
        /// </summary>
        [HttpPost("remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromForm] string clientId)
        {
            await _interaction.RevokeUserConsentAsync(clientId);
            await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));

            return Redirect("/booking-partners/seller-admin");
        }

        /// <summary>
        /// Handle postback to delete a client
        /// </summary>
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] string clientId)
        {
            await _interaction.RevokeUserConsentAsync(clientId);
            await FakeBookingSystem.Database.DeleteBookingPartner(clientId);
            await _events.RaiseAsync(new GrantsRevokedEvent(User.GetSubjectId(), clientId));

            return Redirect("/booking-partners/sys-admin");
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

            await BookingPartnerTable.UpdateScope(clientId, "openid profile openactive-ordersfeed", true);
            return Redirect("/booking-partners/seller-admin");
        }

        /// <summary>
        /// Handle postback to generate a registration key
        /// </summary>
        [HttpPost("regenerate-key")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateKey([FromForm] string clientId)
        {
            var bookingPartner = await BookingPartnerTable.GetByClientId(clientId);
            await BookingPartnerTable.SetKey(clientId, KeyGenerator.GenerateInitialAccessToken(bookingPartner.Name));

            return Redirect($"/booking-partners/edit/{clientId}");
        }

        /// <summary>
        /// Handle postback to generate a registration key, and a new client secret
        /// </summary>
        [HttpPost("reset-client-credentials")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetClientCredentials([FromForm] string clientId)
        {
            var bookingPartner = await BookingPartnerTable.GetByClientId(clientId);
            await BookingPartnerTable.ResetCredentials(clientId, KeyGenerator.GenerateInitialAccessToken(bookingPartner.Name));

            return Redirect($"/booking-partners/edit/{clientId}");
        }

        private static async Task<BookingPartnerModel> BuildBookingPartnerModel(string clientId)
        {
            // var client = await _clients.FindClientByIdAsync(clientId);
            var bookingPartner = await BookingPartnerTable.GetByClientId(clientId);
            if (bookingPartner == null)
                return null;

            return new BookingPartnerModel
            {
                ClientId = bookingPartner.ClientId,
                ClientName = bookingPartner.Name,
                ClientLogoUrl = bookingPartner.LogoUri,
                ClientUrl = bookingPartner.ClientUri,
                BookingPartner = bookingPartner
            };
        }
    }
}