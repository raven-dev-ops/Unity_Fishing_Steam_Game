# Performance Sanity Checklist

## Test Cases
- Harbor idle for 2 minutes.
- Fishing with repeated cast/hook/catch loop.
- Rapid menu open/close while moving focus.

## Metrics
- Average FPS sample via `PerfSanityRunner` logs.
- No runaway frame-time spikes due to UI redraw loops.
- No abnormal memory growth over 10+ minutes.
