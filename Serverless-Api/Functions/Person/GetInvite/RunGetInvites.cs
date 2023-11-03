using Domain;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
    public partial class RunGetInvites
    {
        private readonly Person _user;
        private readonly IPersonService _personService;

        public RunGetInvites(Person user, IPersonService personService)
        {
            _user = user;
            _personService = personService;
        }

        [Function(nameof(RunGetInvites))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "person/invites")] HttpRequestData req)
        {
            Person? person = null;

			try
            {
				person = await _personService.GetInvitesByUserId(_user.Id);
			}
            catch (Exception e)
            {
				return await req.CreateResponse(System.Net.HttpStatusCode.InternalServerError, new { Message = e.Message, Type = "Error" });
			}

            return await req.CreateResponse(System.Net.HttpStatusCode.OK, person.TakeSnapshot());
        }
    }
}
