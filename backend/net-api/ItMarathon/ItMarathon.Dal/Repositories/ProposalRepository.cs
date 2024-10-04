using ItMarathon.Dal.Common;
using ItMarathon.Dal.Common.Contracts;
using ItMarathon.Dal.Context;
using ItMarathon.Dal.Entities;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace ItMarathon.Dal.Repositories;

public class ProposalRepository(ApplicationDbContext repositoryContext) :
    RepositoryBase<Proposal>(repositoryContext), IProposalRepository
{
    public async Task<DataPage<Proposal>> GetProposalsAsync(bool trackChanges, ODataQueryOptions<Proposal> queryOptions)
    {
        IQueryable<Proposal> query = FindAll(trackChanges);
        IQueryable<Proposal> queryFiltered = FindAll(trackChanges);

        var queryOptionParser = new ODataQueryOptionParser(
            model: queryOptions.Context.Model,
            targetEdmType: queryOptions.Context.NavigationSource.EntityType(),
            targetNavigationSource: queryOptions.Context.NavigationSource,
            queryOptions: new Dictionary<string, string> { { "$orderby", "Id" } },
            container: queryOptions.Context.RequestContainer);

        var orderbyOption = new OrderByQueryOption("Id", queryOptions.Context, queryOptionParser);

        if (queryOptions != null)
        {
            query = (IQueryable<Proposal>)queryOptions.ApplyTo(query);

            if (queryOptions.Filter != null)
            {
                queryFiltered = (IQueryable<Proposal>)queryOptions.Filter.ApplyTo(queryFiltered, new ODataQuerySettings());
            }

            if (queryOptions.OrderBy == null)
            {
                query = orderbyOption.ApplyTo(query);
            }
        }
        else
        {
            query = orderbyOption.ApplyTo(query);
        }

        query = query
            .Include(p => p.AppUser)
            .Include(p => p.Photos)
            .Include(p => p.Properties!)
                .ThenInclude(properties => properties.PropertyDefinition)
            .Include(p => p.Properties!)
                .ThenInclude(properties => properties.PredefinedValue)
                    .ThenInclude(prop => prop!.ParentPropertyValue);

        return new DataPage<Proposal>(await query.ToListAsync(),await queryFiltered.CountAsync());
    }

    public async Task<Proposal?> GetProposalAsync(long proposalId, bool trackChanges)
        => await FindByCondition(c => c.Id.Equals(proposalId), trackChanges)
        .Include(p => p.AppUser)
        .Include(p => p.Photos)
        .Include(p => p.Properties!)
            .ThenInclude(properties => properties.PropertyDefinition)
        .Include(p => p.Properties!)
            .ThenInclude(properties => properties.PredefinedValue)
                .ThenInclude(prop => prop!.ParentPropertyValue)
        .SingleOrDefaultAsync();

    public void CreateProposal(Proposal proposal) => Create(proposal);

    public void DeleteProposal(Proposal proposal) => Delete(proposal);
}
