from __future__ import annotations

import asyncio
import time
from collections import deque
from contextlib import asynccontextmanager

from app.errors import RateLimitExceededError


class ProviderGate:
    """Independent concurrency and non-blocking sliding-window rate gate."""

    def __init__(self, max_concurrency: int, requests_per_minute: int) -> None:
        self._semaphore = asyncio.Semaphore(max_concurrency)
        self._requests_per_minute = requests_per_minute
        self._timestamps: deque[float] = deque()
        self._rate_lock = asyncio.Lock()

    @asynccontextmanager
    async def enter(self):
        await self._semaphore.acquire()
        try:
            async with self._rate_lock:
                now = time.monotonic()
                window_start = now - 60.0
                while self._timestamps and self._timestamps[0] < window_start:
                    self._timestamps.popleft()
                if len(self._timestamps) >= self._requests_per_minute:
                    raise RateLimitExceededError()
                self._timestamps.append(now)
            yield
        finally:
            self._semaphore.release()
