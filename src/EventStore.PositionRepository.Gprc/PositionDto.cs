using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventStore.PositionRepository.Gprc;

public class PositionDto
{
    public ulong CommitPosition { get; set; }
    public ulong PreparePosition { get; set; }
}
