class ScraperError(Exception):
    def __init__(self, code: str, safe_message: str, http_status: int = 502):
        super().__init__(safe_message)
        self.code = code
        self.safe_message = safe_message
        self.http_status = http_status


class ProviderTimeoutError(ScraperError):
    def __init__(self) -> None:
        super().__init__("TIMEOUT", "The utility provider request timed out.", 504)


class ProviderUnavailableError(ScraperError):
    def __init__(self) -> None:
        super().__init__(
            "PROVIDER_UNAVAILABLE", "The utility provider is unavailable.", 503
        )


class ProviderParseError(ScraperError):
    def __init__(self) -> None:
        super().__init__(
            "PARSING_ERROR", "The utility provider response could not be parsed.", 502
        )


class InvalidAccountError(ScraperError):
    def __init__(self, message: str = "The utility account number is invalid.") -> None:
        super().__init__("INVALID_ACCOUNT", message, 422)


class RateLimitExceededError(ScraperError):
    def __init__(self) -> None:
        super().__init__(
            "RATE_LIMITED", "The provider request rate limit was reached.", 429
        )
