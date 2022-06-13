using Hmm.BigCalendar.Contract;
using Hmm.BigCalendar.DomainEntity;
using Hmm.BigCalendar.Managers;
using Hmm.BigCalendar.Validators;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Hmm.Utility.Dal.Query;
using Xunit;

namespace Hmm.BigCalendar.Test
{
    public class AppointmentManagerTests : IDisposable
    {
        private readonly DateTime _currentDateTime = new(2022, 1, 2);
        private List<Appointment> _appoints;
        private readonly IAppointmentManager _appointMan;

        public AppointmentManagerTests()
        {
            _appoints = new List<Appointment>();
            var mockRepo = new Mock<IGuidRepository<Appointment>>();
            mockRepo.Setup(a => a.Add(It.IsAny<Appointment>())).Returns((Appointment appointment) =>
            {
                appointment.Id = Guid.NewGuid();
                _appoints ??= new List<Appointment>();
                _appoints.Add(appointment);
                return appointment;
            });
            mockRepo.Setup(a => a.GetEntity(It.IsAny<Guid>())).Returns((Guid id) =>
            {
                _appoints ??= new List<Appointment>();
                var rec = _appoints.FirstOrDefault(a => a.Id == id);
                return rec;
            });
            mockRepo.Setup(a => a.GetEntities(It.IsAny<Expression<Func<Appointment, bool>>>(), It.IsAny<ResourceCollectionParameters>())).Returns(
                (Expression<Func<Appointment, bool>> query) =>
                {
                    _appoints ??= new List<Appointment>();
                    return query != null ? _appoints.AsQueryable().Where(query) : _appoints.AsQueryable();
                });
            var fakeDate = new Mock<IDateTimeProvider>();
            fakeDate.Setup(d => d.UtcNow).Returns(_currentDateTime);
            var validator = new DefaultAppointmentValidator(fakeDate.Object);
            _appointMan = new DefaultAppointmentManager(mockRepo.Object, validator);
        }

        public void Dispose()
        {
            _appoints = new List<Appointment>();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void Can_Get_DaysOfYear()
        {
        }

        [Fact]
        public void Can_Add_New_Appointment()
        {
            // Arrange
            var appoint = new Appointment
            {
                HostId = Guid.NewGuid(),
                StartTime = new DateTime(2022, 3, 10),
                EndTime = new DateTime(2022, 4, 10),
                Contact = new ContactInfo()
            };

            // Act
            var result = _appointMan.Create(appoint);
            var savedRec = _appointMan.GetAppointmentById(result.Id);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(savedRec);
            Assert.NotEqual(result.Id, Guid.Empty);
            Assert.True(_appointMan.ProcessResult.Success);
        }

        [Fact]
        public void Can_Cancel_Exists_Appointment()
        {
            // Arrange
            var id = Guid.NewGuid();
            var appoint = new Appointment { Id = id };
            _appoints = new List<Appointment> { appoint };

            // Act
            var canceledAppoint = _appointMan.Cancel(appoint);

            // Assert
            Assert.NotNull(canceledAppoint);
            Assert.True(canceledAppoint.Cancelled);
            Assert.True(_appointMan.ProcessResult.Success);
        }

        [Fact]
        public void Can_Modify_Exists_Appointment()
        {
            // Arrange
            var appoint = new Appointment
            {
                HostId = Guid.NewGuid(),
                StartTime = new DateTime(2022, 12, 10),
                EndTime = new DateTime(2022, 12, 15),
                Contact = new ContactInfo()
            };
            var savedAppoint = _appointMan.Create(appoint);
            var newStartDate = new DateTime(2022, 4, 1);
            savedAppoint.StartTime = newStartDate;

            // Act
            var updatedAppoint = _appointMan.Update(savedAppoint);

            // Assert
            Assert.NotNull(updatedAppoint);
            Assert.Equal(updatedAppoint.StartTime, newStartDate);
            Assert.True(_appointMan.ProcessResult.Success);
        }

        [Fact]
        public void Can_Get_Exists_Appointment_ByID()
        {
            // Arrange
            var appoint = new Appointment
            {
                Id = Guid.NewGuid(),
                StartTime = new DateTime(2022, 2, 10),
                EndTime = new DateTime(2022, 2, 15)
            };
            _appoints.Add(appoint);
            var id = appoint.Id;

            // Act
            var appointFound = _appointMan.GetAppointmentById(id);

            // Assert
            Assert.NotNull(appointFound);
            Assert.Equal(appointFound.Id, id);
            Assert.True(_appointMan.ProcessResult.Success);
        }

        [Fact]
        public void Can_Search_Exists_Appointment()
        {
        }

        [Fact]
        public void Can_List_Exists_Valid_Appointment_ByTimeRange()
        {
            // Arrange
            SetSampleAppointments();
            var startDate = new DateTime(2022, 3, 1);
            var endDate = new DateTime(2022, 5, 1);

            // Act
            var appoints = _appointMan.GetAppointmentsByDateRange(startDate, endDate).ToList();

            // Assert
            Assert.NotNull(appoints);
            Assert.True(appoints.Any());
            Assert.Contains(appoints, a => a.StartTime.Month == 3);
            Assert.DoesNotContain(appoints, a => a.StartTime.Month < 3);
            Assert.DoesNotContain(appoints, a => a.StartTime.Month >= 5);
            Assert.True(_appointMan.ProcessResult.Success);
        }

        [Fact]
        public void Can_List_All_Exists_Valid_Appointment()
        {
            // Arrange
            SetSampleAppointments();

            // Act
            var appoints = _appointMan.GetAppointments().ToList();

            // Assert
            Assert.NotNull(appoints);
            Assert.True(appoints.Any());
            Assert.Equal(appoints.Count, _appoints.Count);
        }

        private void SetSampleAppointments()
        {
            var hostId1 = Guid.NewGuid();
            var hostId2 = Guid.NewGuid();
            _appoints = new List<Appointment>
            {
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 2, 12),
                    EndTime = new DateTime(2022, 2, 13), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 3, 12),
                    EndTime = new DateTime(2022, 3, 13), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 3, 20),
                    EndTime = new DateTime(2022, 3, 21), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 3, 24),
                    EndTime = new DateTime(2022, 3, 25), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 4, 1),
                    EndTime = new DateTime(2022, 4, 2), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 4, 20),
                    EndTime = new DateTime(2022, 4, 21), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 4, 25),
                    EndTime = new DateTime(2022, 4, 26), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 5, 25),
                    EndTime = new DateTime(2022, 5, 26), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 6, 2),
                    EndTime = new DateTime(2022, 6, 3), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId1, StartTime = new DateTime(2022, 6, 25),
                    EndTime = new DateTime(2022, 6, 26), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId2, StartTime = new DateTime(2022, 5, 10),
                    EndTime = new DateTime(2022, 5, 26), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId2, StartTime = new DateTime(2022, 6, 2),
                    EndTime = new DateTime(2022, 6, 3), Contact = new ContactInfo()
                },
                new()
                {
                    Id = Guid.NewGuid(), HostId = hostId2, StartTime = new DateTime(2022, 6, 25),
                    EndTime = new DateTime(2022, 6, 26), Contact = new ContactInfo()
                }
            };
        }
    }
}