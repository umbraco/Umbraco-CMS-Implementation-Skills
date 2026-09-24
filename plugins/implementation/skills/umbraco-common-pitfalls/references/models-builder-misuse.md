# Models Builder misuse

**Symptoms:** generated models that are slow to use, a bloated content cache, hard-to-follow
view-model logic.
**Look for:** ModelsBuilder partial classes that inject services, traverse the content tree, or
build view models.
**Docs section:** "Best practices when using Models Builder"

Use ModelsBuilder partial classes to add **stateless, local** features — computed properties
derived from the model's own data. Do not:

- Transform content into view models inside a ModelsBuilder partial
- Resolve and store related content as properties
- Manage or traverse content trees

These concerns belong in controllers, view components, or services.

**Related:** [Eager loading](eager-loading.md).
