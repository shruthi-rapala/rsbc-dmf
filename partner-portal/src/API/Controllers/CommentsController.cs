﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Rsbc.Dmf.CaseManagement.Service;
using Rsbc.Dmf.PartnerPortal.Api.Services;
using SharedUtils;
using System.Net;
using static Rsbc.Dmf.CaseManagement.Service.CaseManager;

namespace Rsbc.Dmf.PartnerPortal.Api.Controllers
{
    [Route("api/[controller]")]
    //[Authorize(Policy = Policy.Driver)]
    [ApiController]
    public class CommentsController : Controller
    {
        private readonly CallbackManager.CallbackManagerClient _callbackManagerClient;
        private readonly CaseManagerClient _caseManagerClient;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public CommentsController(CallbackManager.CallbackManagerClient callbackManagerClient, CaseManager.CaseManagerClient caseManagerClient, IUserService userService, IMapper mapper)
        {
            _callbackManagerClient = callbackManagerClient;
            _caseManagerClient = caseManagerClient;
            _userService = userService;
            _mapper = mapper;
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(IEnumerable<ViewModels.CaseCallback>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [ActionName(nameof(GetCaseComments))]
        public async Task<ActionResult> GetCaseComments()
        {
            var result = new List<ViewModels.Comment>();

            var profile = await _userService.GetCurrentUserContext();

            var commentsRequest = new DriverLicenseRequest { DriverLicenseNumber = profile.DriverLicenseNumber };
            var getComments = _caseManagerClient.GetAllDriverComments(commentsRequest);
            if (getComments?.ResultStatus == ResultStatus.Success)
            {
                result = _mapper.Map<List<ViewModels.Comment>>(getComments.Items);
            }
            else
            {
                return StatusCode(500, getComments?.ErrorDetail ?? $"{nameof(getComments)} failed.");
            }

            return Json(result);
        }

    }
}