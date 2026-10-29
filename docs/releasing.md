# Releasing

The three libraries are published to nuget.org as packages with the same version: `DK.Gov.Oio.Saml`, `DK.Gov.Oio.Saml.AspNetCore` and `DK.Gov.Oio.Saml.AspNet`. A release is a version tag on `main`. The [release workflow](../.github/workflows/release.yml) tests the tagged commit, packs the packages on Windows (the .NET Framework package builds only there), and publishes them once a reviewer approves.

## One-time setup

**On nuget.org**, as Digitaliseringsstyrelsen's organisation, which already owns `dk.nita.saml20`:

1. **Reserve the ID prefix `DK.Gov.Oio`** for the organisation by writing to account@nuget.org. Packages under the prefix then show as verified, and nobody else can publish one. Publishing doesn't have to wait for it; ask for the reservation to cover the packages the organisation has already published.
2. **Add a Trusted Publishing policy** (account settings, *Trusted Publishing*), signed in as a member of the organisation: package owner the organisation, repository owner `digst`, repository `oiosaml-net`, workflow file `release.yml`, environment `nuget`. The workflow then gets a short-lived API key for each release, and the packages belong to the organisation; no key is stored in GitHub.
3. **Keep at least one Digitaliseringsstyrelsen employee as an administrator** of the organisation.

**On GitHub**, in the repository settings:

1. **Create the environment `nuget`**, with required reviewers and a deployment rule that allows only tags matching `v*`.
2. **Add the variable `NUGET_USER`** to it: the nuget.org profile name (not the email address) of the member who created the policy. When that member leaves the organisation, another member creates the policy again and `NUGET_USER` changes to their profile name.

## Versions

Versions follow [Semantic Versioning](https://semver.org/) and come from the tag: `v1.2.3` publishes 1.2.3, `v1.3.0-rc.1` a pre-release. A local build is version 1.0.0; the tag is the only source of a released version.

The public API is pinned by `PublicApiTests`. Removing or changing a public type or member is a breaking change and needs a new major version; adding one is a minor version.

## Releasing a version

1. Check that CI on `main` is green.
2. Draft the GitHub release for the coming tag, with the release notes: what changed, and for a major version what integrators must change.
3. Tag `main` and push the tag:

   ```bash
   git switch main && git pull
   git tag -a v1.0.0 -m "DK.Gov.Oio.Saml 1.0.0"
   git push origin v1.0.0
   ```

4. The release workflow runs the tests, checks that the tag is on `main` and is a valid version, and packs. It then waits for approval in the `nuget` environment. Approve the deployment in the workflow run.
5. The packages appear on nuget.org after its validation, usually within minutes. Publish the GitHub release.

## When something goes wrong

- **Before approval:** cancel the workflow run, delete the tag (`git push origin :refs/tags/v1.0.0`), fix, and tag again.
- **After publishing:** nuget.org never lets a version be deleted or replaced. Unlist the broken version on nuget.org and release a new patch version.

## After 1.0.0

Deprecate `dk.nita.saml20` and its `dk.nita.saml20.ext.*` packages on nuget.org, with `DK.Gov.Oio.Saml.AspNet` as the alternative, and link to the [migration guide](migration-from-dk.nita.saml20.md).
