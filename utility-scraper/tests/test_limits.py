import asyncio

import pytest

from app.errors import RateLimitExceededError
from app.limits import ProviderGate


def test_provider_gate_rejects_requests_over_the_sliding_window_limit():
    async def scenario():
        gate = ProviderGate(max_concurrency=1, requests_per_minute=1)
        async with gate.enter():
            pass
        with pytest.raises(RateLimitExceededError):
            async with gate.enter():
                pass

    asyncio.run(scenario())
