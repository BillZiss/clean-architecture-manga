namespace Application.UseCases
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Application.Boundaries.GetCustomerDetails;
    using Domain.Accounts;
    using Domain.Accounts.ValueObjects;
    using Domain.Customers;
    using Domain.Customers.ValueObjects;
    using Domain.Security.Services;
    using Domain.Security.ValueObjects;

    /// <summary>
    /// Get Customer Details <see href="https://github.com/ivanpaulovich/clean-architecture-manga/wiki/Domain-Driven-Design-Patterns#use-case">Use Case Domain-Driven Design Pattern</see>.
    /// </summary>
    public sealed class GetCustomerDetails : IUseCase
    {
        private readonly IUserService _userService;
        private readonly IOutputPort _outputPort;
        private readonly ICustomerRepository _customerRepository;
        private readonly IAccountRepository _accountRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetCustomerDetails"/> class.
        /// </summary>
        /// <param name="userService">User Service.</param>
        /// <param name="outputPort">Output Port.</param>
        /// <param name="customerRepository">Customer Repository.</param>
        /// <param name="accountRepository">Account Repository.</param>
        public GetCustomerDetails(
            IUserService userService,
            IOutputPort outputPort,
            ICustomerRepository customerRepository,
            IAccountRepository accountRepository)
        {
            this._userService = userService;
            this._outputPort = outputPort;
            this._customerRepository = customerRepository;
            this._accountRepository = accountRepository;
        }

        /// <summary>
        /// Executes the Use Case.
        /// </summary>
        /// <param name="input">Input Message.</param>
        /// <returns>Task.</returns>
        public async Task Execute(GetCustomerDetailsInput input)
        {
            ICustomer customer;

            if (this._userService.GetCustomerId() is CustomerId customerId)
            {
                try
                {
                    customer = await this._customerRepository.GetBy(customerId);
                }
                catch (CustomerNotFoundException ex)
                {
                    this._outputPort.NotFound(ex.Message);
                    return;
                }
            }
            else
            {
                this._outputPort.NotFound("Customer does not exist.");
                return;
            }

            var accounts = new List<Boundaries.GetCustomerDetails.Account>();

            foreach (AccountId accountId in customer.Accounts.GetAccountIds())
            {
                IAccount account;

                try
                {
                    account = await this._accountRepository.Get(accountId);
                }
                catch (AccountNotFoundException ex)
                {
                    this._outputPort.NotFound(ex.Message);
                    return;
                }

                var outputAccount = new Boundaries.GetCustomerDetails.Account(account);
                accounts.Add(outputAccount);
            }

            this.BuildOutput(
                this._userService.GetExternalUserId(),
                customer,
                accounts);
        }

        private void BuildOutput(
            ExternalUserId externalUserId,
            ICustomer customer,
            List<Boundaries.GetCustomerDetails.Account> accounts)
        {
            var output = new GetCustomerDetailsOutput(
                externalUserId,
                customer,
                accounts);
            this._outputPort.Standard(output);
        }
    }
}
