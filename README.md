A sample program to explore a classic producer problem using .NET
===

Problem:
===
We have an eventhub producing a steady stream of events and we need to be able to read the stream with a throttler to read only N items per limiting period.

Solution:
===
We read directly from the stream into a bounded channel. This allows us to include a broker that can be used to broker between many producers and consumers. On the consumer, we use a semaphore as a throttler.

A semaphore can be used for throttling in 2 ways:

    a. Throttle based on a sliding window. i.e We process the entire set of operation such that we wait the cooldown between processing the sets
        e.g. 
        time(Item 2.a) -  time(Item 1.a) = Cooldown period
        time(Item 2.b) -  time(Item 1.b) = Cooldown period
        time(Item 2.c) -  time(Item 1.c) = Cooldown period

    b. Throttle based on a tumbling window. i.e. We process N items per the wallclock time.
        e.g. 
        time(Item 2.a) -  time(Item 1.a) = time(Item 1.a) - time(nextInterval)

Current implementation explores option a, processing 2 items per minute:

`
8/23/2020 6:21:40 PM - {"messageId":38,"deviceId":"Raspberry Pi Web Client","temperature":23.565528395029407,"humidity":74.90540809878516}
8/23/2020 6:21:50 PM - {"messageId":39,"deviceId":"Raspberry Pi Web Client","temperature":20.911399297984136,"humidity":69.4725415709778}
8/23/2020 6:22:40 PM - {"messageId":40,"deviceId":"Raspberry Pi Web Client","temperature":24.778757019544827,"humidity":70.01440148978234}
8/23/2020 6:22:50 PM - {"messageId":41,"deviceId":"Raspberry Pi Web Client","temperature":27.887049690089636,"humidity":72.90843142465644}
8/23/2020 6:23:40 PM - {"messageId":42,"deviceId":"Raspberry Pi Web Client","temperature":20.521722046301917,"humidity":73.20191311278155}
8/23/2020 6:23:50 PM - {"messageId":43,"deviceId":"Raspberry Pi Web Client","temperature":25.98449527798854,"humidity":77.24486608688234}
8/23/2020 6:24:40 PM - {"messageId":44,"deviceId":"Raspberry Pi Web Client","temperature":29.520121486584138,"humidity":60.62681328571299}
8/23/2020 6:24:50 PM - {"messageId":45,"deviceId":"Raspberry Pi Web Client","temperature":25.275473093986022,"humidity":60.15165859838731}
8/23/2020 6:25:40 PM - {"messageId":46,"deviceId":"Raspberry Pi Web Client","temperature":23.060287210340118,"humidity":67.36455645991032}
`