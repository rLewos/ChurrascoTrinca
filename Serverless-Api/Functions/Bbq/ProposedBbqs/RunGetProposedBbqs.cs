using System.Net;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
    public partial class RunGetProposedBbqs
    {
        private readonly Person _user;
        private readonly IBbqRepository _bbqRepository;
        private readonly IPersonRepository _personRepository;
        public RunGetProposedBbqs(IPersonRepository personRepository, IBbqRepository bbqRepository, Person user)
        {
            _user = user;
			_bbqRepository = bbqRepository;
			_personRepository = personRepository;
        }

        [Function(nameof(RunGetProposedBbqs))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras")] HttpRequestData req)
        {
            var snapshots = new List<object>();
            var moderator = await _personRepository.GetAsync(_user.Id);
            foreach (var bbqId in moderator.Invites.Where(i => i.Date > DateTime.Now).Select(o => o.Id).ToList())
            {
                var bbq = await _bbqRepository.GetAsync(bbqId);
                if (bbq == null)
                    throw new Exception("Bbq not found.");
                    
                if(bbq.Status != BbqStatus.ItsNotGonnaHappen)
                    snapshots.Add(bbq.TakeSnapshot());
            }

            return await req.CreateResponse(HttpStatusCode.Created, snapshots);
        }
    }
}
