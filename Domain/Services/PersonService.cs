using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using Eveneum;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Services
{
	public class PersonService : IPersonService
	{
		private readonly IPersonRepository _personRepository;
		private readonly IBbqRepository _bbqRepository;
        public PersonService(IPersonRepository personRepository, IBbqRepository bbqRepository)
        {
			_personRepository = personRepository;
			_bbqRepository = bbqRepository;
        }

		public async Task<Person?> AcceptInvite(string id, string inviteId, bool isVeg)
		{
			var person = await this.GetAsync(id);

			if (person == null)
				throw new Exception("Person not found.");

			if (person.Invites.Any(x => x.Id == inviteId && x.Status == InviteStatus.Accepted))
				throw new Exception("Invite already accepted");

			person.Apply(new InviteWasAccepted { InviteId = inviteId, IsVeg = isVeg, PersonId = person.Id });
			await this.SaveAsync(person);

			//implementar efeito do aceite do convite no churrasco
			Bbq? bbq = await _bbqRepository.GetAsync(inviteId);
			if (bbq == null)
				throw new Exception("Bbq not found.");

			var @event = new PersonHasConfirmed { BbqID = bbq.Id, PersonID = person.Id, IsVeg = isVeg };
			bbq.Apply(@event);
			await _bbqRepository.SaveAsync(bbq);

			return person;
		}

		public async Task<Person?> DeclineInvite(string id, string inviteId)
		{
			var person = await this.GetAsync(id);

			if (person == null)
				throw new Exception("Person not found");

			person.Apply(new InviteWasDeclined { InviteId = inviteId, PersonId = person.Id });
			await this.SaveAsync(person);

			//Implementar impacto da recusa do convite no churrasco caso ele já tivesse sido aceito antes
			Bbq? bbq = await _bbqRepository.GetAsync(inviteId);
			if (bbq == null)
				throw new Exception("Bbq not found");

			var @event = new InviteWasDeclined() { InviteId = inviteId, PersonId = person.Id };
			bbq.Apply(@event);
			await _bbqRepository.SaveAsync(bbq);

			return person;
		}

		public async Task<Person?> GetAsync(string streamId)
		{
			return await _personRepository.GetAsync(streamId);
		}

		public async Task<StreamHeaderResponse> GetHeaderAsync(string streamId)
		{
			return await _personRepository.GetHeaderAsync(streamId);
		}

		public async Task<Person?> GetInvitesByUserId(string id)
		{
			var person = await this.GetAsync(id);

			if (person == null)
				throw new Exception("Person not found");

			return person;
		}

		public async Task SaveAsync(Person entity)
		{
			await _personRepository.SaveAsync(entity);
		}
	}
}
