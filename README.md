# Extension method for dynamically including navigation properties in EF Core

## Simple use case
DynamicInclude:

	context.Owners.DynamicInclude("Pets.Foods")
	
>DynamicInclude is case insensitive

	// Will also work
	context.Owners.DynamicInclude("pets.foods")	
	
	
Normal EF Core Equivalent:

	context.Owners
		.Include(o => o.Pets)
			.ThenInclude(p => p.Foods)

## Deep nested multiple includes (however deep you want)

>If you experience performance issues, consider adding AsSplitQuery() to query

DynamicInclude:

	context.Owners.DynamicInclude("Pets.(Snacks, Toys.Manufacturer)")

Normal EF Core Equivalent:

	context.Owners
		.Include(o => o.Pets)
			.ThenInclude(p => p.Snacks)
		.Include(o => o.Pets)
			.ThenInclude(p => p.Toys)
				.ThenInclude(t => t.Manufacturer)
