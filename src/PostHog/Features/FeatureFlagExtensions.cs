using PostHog.Features;

namespace PostHog; // Intentionally put in the root namespace.

/// <summary>
/// Extensions of <see cref="IPostHogClient"/> specific to feature flag evaluation.
/// </summary>
public static class FeatureFlagExtensions
{
    /// <summary>
    /// Determines whether a feature is enabled for the specified user.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="personProperties">Optional: What person properties are known. Used to compute flags locally, if personalApiKey is present. Not needed if using remote evaluation.</param>
    /// <param name="groupProperties">Optional: A list of the currently active groups. Required if the flag depends on groups. Each group can optionally include properties that override what's on PostHog's server when evaluating feature flags. Specifing properties for each group is required if local evaluation is <c>true</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature is undefined.
    /// </returns>
    public static async Task<bool?> IsFeatureEnabledAsync(this IPostHogClient client,
        string distinctId,
        string featureKey,
        Dictionary<string, object>? personProperties,
        GroupCollection? groupProperties,
        CancellationToken cancellationToken)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));
        var result = await client.GetFeatureFlagAsync(
            distinctId,
            featureKey,
            personProperties,
            groupProperties,
            sendFeatureFlagEvents: true,
            cancellationToken);

        return result?.IsEnabled;
    }

    /// <summary>
    /// Determines whether a feature is enabled for the specified user.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature does not
    /// exist.
    /// </returns>
    public static Task<bool?> IsFeatureEnabledAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey,
        CancellationToken cancellationToken)
        => client.IsFeatureEnabledAsync(
            distinctId,
            featureKey,
            personProperties: null,
            groupProperties: null,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Determines whether a feature is enabled for the specified user.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature is undefined.
    /// </returns>
    public static Task<bool?> IsFeatureEnabledAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey)
        => client.IsFeatureEnabledAsync(
            distinctId,
            featureKey,
            personProperties: null,
            groupProperties: null,
            cancellationToken: CancellationToken.None);

    /// <summary>
    /// Determines whether a feature is enabled for the specified user.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="personProperties">Optional: What person properties are known. Used to compute flags locally, if personalApiKey is present. Not needed if using remote evaluation.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature is undefined.
    /// </returns>
    public static Task<bool?> IsFeatureEnabledAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey,
        Dictionary<string, object> personProperties)
        => client.IsFeatureEnabledAsync(
            distinctId,
            featureKey,
            personProperties,
            groupProperties: null,
            cancellationToken: CancellationToken.None);

    /// <summary>
    /// Retrieves a feature flag.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="personProperties">Optional: What person properties are known. Used to compute flags locally, if personalApiKey is present. Not needed if using remote evaluation.</param>
    /// <param name="groupProperties">Optional: A list of the currently active groups. Required if the flag depends on groups. Each group can optionally include properties that override what's on PostHog's server when evaluating feature flags. Specifing properties for each group is required if local evaluation is <c>true</c>.</param>
    /// <param name="sendFeatureFlagEvents">Default <c>true</c>. If <c>true</c>, this method captures a $feature_flag_called event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The feature flag or null if it does not exist or is not enabled.</returns>
    public static async Task<FeatureFlag?> GetFeatureFlagAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey,
        Dictionary<string, object>? personProperties,
        GroupCollection? groupProperties,
        bool sendFeatureFlagEvents,
        CancellationToken cancellationToken)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));
        var flags = await client.GetFeatureFlagsAsync(
            distinctId,
            personProperties,
            groupProperties,
            cancellationToken);

        var flag = flags.GetValueOrDefault(featureKey);

        if (sendFeatureFlagEvents)
        {
            object flagResponse = flag.ToResponseObject();

            client.CaptureEvent(
                distinctId,
                eventName: "$feature_flag_called",
                properties: new Dictionary<string, object>
                {
                    ["$feature_flag"] = featureKey,
                    ["$feature_flag_response"] = flagResponse,
                    ["locally_evaluated"] = false,
                    [$"$feature/{featureKey}"] = flagResponse
                });
        }

        return flags.GetValueOrDefault(featureKey);
    }

    /// <summary>
    /// Retrieves all the feature flags.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="personProperties">Optional: What person properties are known. Used to compute flags locally, if personalApiKey is present. Not needed if using remote evaluation.</param>
    /// <param name="groupProperties">Optional: A list of the currently active groups. Required if the flag depends on groups. Each group can optionally include properties that override what's on PostHog's server when evaluating feature flags. Specifing properties for each group is required if local evaluation is <c>true</c>.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature is undefined.
    /// </returns>
    public static async Task<IReadOnlyDictionary<string, FeatureFlag>> GetFeatureFlagsAsync(
        this IPostHogClient client,
        string distinctId,
        Dictionary<string, object>? personProperties,
        GroupCollection? groupProperties)
        => await (client ?? throw new ArgumentNullException(nameof(client)))
            .GetFeatureFlagsAsync(distinctId, personProperties, groupProperties, CancellationToken.None);

    /// <summary>
    /// Retrieves a feature flag.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The feature flag or null if it does not exist or is not enabled.</returns>
    public static async Task<FeatureFlag?> GetFeatureFlagAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey,
        CancellationToken cancellationToken)
        => await client.GetFeatureFlagAsync(
            distinctId,
            featureKey,
            personProperties: null,
            groupProperties: null,
            sendFeatureFlagEvents: true,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Retrieves a feature flag.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <returns>The feature flag or null if it does not exist or is not enabled.</returns>
    public static async Task<FeatureFlag?> GetFeatureFlagAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey)
        => await client.GetFeatureFlagAsync(
            distinctId,
            featureKey,
            personProperties: null,
            groupProperties: null,
            sendFeatureFlagEvents: true,
            cancellationToken: CancellationToken.None);

    /// <summary>
    /// When reporting the result of a feature flag evaluation, this method converts the result to a string
    /// in a format expected by the Capture event api.
    /// </summary>
    /// <param name="featureFlag">The feature flag.</param>
    /// <returns>A string with either the variant key or true/false.</returns>
    internal static object ToResponseObject(this FeatureFlag? featureFlag)
        => featureFlag is not null
            ? featureFlag.VariantKey ?? (object)featureFlag.IsEnabled
            : "undefined";
}