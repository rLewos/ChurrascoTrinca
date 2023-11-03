using Eveneum;
using System.Net;
using CrossCutting;
using Domain.Events;
using Domain.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Domain.Repositories;
using Domain;
using Domain.Services;

namespace Serverless_Api
{
    public partial class RunCreateNewBbq
    {
        private readonly Person _user;
        private readonly SnapshotStore _snapshots;
        
        private readonly IPersonService _personService;
        private readonly IBbqService _bbqService;

		public RunCreateNewBbq(IPersonService personService, IBbqService bbqService, SnapshotStore snapshots, Person user)
		{
			_user = user;
			_snapshots = snapshots;
			_personService = personService;
			_bbqService = bbqService;
		}

		[Function(nameof(RunCreateNewBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "churras")] HttpRequestData req)
        {
			try
			{
				var input = await req.Body<NewBbqRequest>();
				if (input == null)
					return await req.CreateResponse(HttpStatusCode.BadRequest, "input is required.");

				Bbq? bbq = await _bbqService.CreateNew(input.Date, input.Reason, input.IsTrincasPaying);
				var churrasSnapshot = bbq.TakeSnapshot();
				return await req.CreateResponse(HttpStatusCode.Created, churrasSnapshot);
			}
			catch (Exception e)
			{

				return await req.CreateResponse(System.Net.HttpStatusCode.InternalServerError, new { Message = e.Message, Type = "Error" });
			}


        }
    }
}
