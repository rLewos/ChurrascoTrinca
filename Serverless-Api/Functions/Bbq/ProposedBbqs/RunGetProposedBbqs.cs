using System.Net;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
    public partial class RunGetProposedBbqs
    {
        private readonly Person _user;
		private readonly IPersonService _personService;
		private readonly IBbqService _bbqService;

		public RunGetProposedBbqs(IPersonService personService, IBbqService bbqService, Person user)
        {
            _user = user;
            _personService = personService;
            _bbqService = bbqService;
        }

        [Function(nameof(RunGetProposedBbqs))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras")] HttpRequestData req)
        {
            var snapshots = new List<object>();

            try
            {
				snapshots = await _bbqService.GetAllNotRejectedOrAvailable(_user.Id);
			}
            catch (Exception e)
            {

				return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
			}

            return await req.CreateResponse(HttpStatusCode.Created, snapshots);
        }
    }
}
