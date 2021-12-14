# Extension method for dynamically including navigation properties in EF Core

## Deep nested includes

Syntax:

	// Single deep include
	includePropName1.includePropName2
	
	// Multiple child includes
	includePropName1.(includePropName2, includePropName3)

Example:

	context.Owners.DynamicInclude("Pets.(Snacks, Toys.Manufacturer)")
	
>DynamicInclude is **case-insensitive**

	// will also work
	context.Owners.DynamicInclude("pets.(sNacks, tOys.manuFacturer)");

Normal EF Core equivalent:

	context.Owners
		.Include(o => o.Pets)
			.ThenInclude(p => p.Snacks)
		.Include(o => o.Pets)
			.ThenInclude(p => p.Toys)
				.ThenInclude(t => t.Manufacturer)


## Adding OrderBy to child includes

Syntax:

	includePropname<[+-]orderPropName1, [+-]orderPropName2>
	
Example:

	context.Owners.DynamicInclude("Pets<Name, -Weight>.Snacks<Id>")

Normal EF Core equivalent:

	context.Owners
		.Include(o => o.Pets.OrderBy(p => p.Name).ThenByDescending(o => p.Weight))
			ThenInclude(p => p.Snacks.OrderBy(sn => sn.Id))

## Skipping/Taking in child includes

Syntax:

	// Take t elements
	includePropName[:t]
	
	// Skip s elements
	includePropName[s:]
	
	// Skip s and take (t - s) elements
	includePropName[s:t]

Example:

	context.Owners.DynamicInclude("Pets<-Weight>[:3].Snacks[1:4]")
	
Normal EF Core equivalent:

	context.Owners
		.Include(o => o.Pets.OrderByDescending(p => p.Weights).Take(3))
			ThenInclude(p => p.Snacks.Skip(1).Take(3))

## Note
>If you experience performance issues, consider adding AsSplitQuery() to query
