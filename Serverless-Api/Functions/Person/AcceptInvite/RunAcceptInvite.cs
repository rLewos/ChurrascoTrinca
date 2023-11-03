using Domain.Events;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Domain.Services;

namespace Serverless_Api
{
	public partial class RunAcceptInvite
	{
		private readonly Person _user;
		private readonly IPersonService _personService;
		private readonly IBbqService _bbqService;

		public RunAcceptInvite(IPersonService personService, IBbqService bbqService, Person user)
		{
			_user = user;
			_personService = personService;
			_bbqService = bbqService;
		}

		[Function(nameof(RunAcceptInvite))]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "person/invites/{inviteId}/accept")] HttpRequestData req, string inviteId)
		{
			var answer = await req.Body<InviteAnswer>();
			Person? person = null;
			try
			{
				if(string.IsNullOrEmpty(inviteId))
					throw new ArgumentNullException("InviteId is null");

				person = await _personService.AcceptInvite(_user.Id, inviteId, answer.IsVeg);
			}
			catch (Exception e)
			{

				return await req.CreateResponse(System.Net.HttpStatusCode.NoContent, new { Message = e.Message, Type = "Error" });
			}


			return await req.CreateResponse(System.Net.HttpStatusCode.OK, person.TakeSnapshot());
		}
	}
}
