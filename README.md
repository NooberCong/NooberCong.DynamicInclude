# Extension method for dynamically including navigation properties in EF Core

## Simple use case
DynamicInclude:

	context.Owners.Include("Pets.Foods")
	
	Or if you worry about name changes
	
	context.Owners.Include($"{nameof(Owner.Pets)}.{nameof(Pet.Foods)}")

	
Normal EF Core Equivalent:

	context.Owners
		.Include(o => o.Pets)
			.ThenInclude(p => p.Foods)

## Deep nested multiple includes (however deep you want)

>If you have performance issues, add AsSplitQuery() before using dynamicInclude

DynamicInclude:

	context.Owners.Include("Pets.(Snacks, Toys.Manufacturer)")

Normal EF Core Equivalent:

	context.Owners
		.Include(o => o.Pets)
			.ThenInclude(p => p.Snacks)
		.Include(o => o.Pets)
			.ThenInclude(p => p.Toys)
				.ThenInclude(t => t.Manufacturer)
