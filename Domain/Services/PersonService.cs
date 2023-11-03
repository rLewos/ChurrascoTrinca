using Domain.Entities;
using Domain.Repositories;
using Eveneum;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Services
{
	public class PersonService : IPersonService
	{
		private readonly IPersonRepository _personRepository;
        public PersonService(IPersonRepository personRepository)
        {
			_personRepository = personRepository;
        }
        public async Task<Person?> GetAsync(string streamId)
		{
			return await _personRepository.GetAsync(streamId);
		}

		public async Task<StreamHeaderResponse> GetHeaderAsync(string streamId)
		{
			return await _personRepository.GetHeaderAsync(streamId);
		}

		public async Task SaveAsync(Person entity)
		{
			await _personRepository.SaveAsync(entity);
		}
	}
}
