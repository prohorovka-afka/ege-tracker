using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    public class TrackerViewModel : ViewModelBase
    {
        public ObservableCollection<Subject> Subjects { get; } = new();

        private Subject? _selectedSubject;
        public Subject? SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (SetField(ref _selectedSubject, value))
                {
                    OnPropertyChanged(nameof(SelectedTopicList));
                    OnPropertyChanged(nameof(SelectedSubjectIsEge));
                }
            }
        }

        public ObservableCollection<Topic> SelectedTopicList =>
            SelectedSubject?.Topics ?? new ObservableCollection<Topic>();

        public bool SelectedSubjectIsEge => SelectedSubject?.IsEge ?? false;

        public string NewSubjectName { get; set; } = string.Empty;
        public string NewTopicName { get; set; } = string.Empty;

        public ICommand AddSubjectCommand { get; }
        public ICommand RemoveSubjectCommand { get; }
        public ICommand AddTopicCommand { get; }
        public ICommand RemoveTopicCommand { get; }
        public ICommand RefreshCommand { get; }

        public TrackerViewModel()
        {
            foreach (var s in App.ProgressService.Subjects)
                Subjects.Add(s);

            SelectedSubject = Subjects.FirstOrDefault();

            AddSubjectCommand    = new RelayCommand(_ => AddSubject());
            RemoveSubjectCommand = new RelayCommand(p => RemoveSubject(p as Subject));
            AddTopicCommand      = new RelayCommand(_ => AddTopic());
            RemoveTopicCommand   = new RelayCommand(p => RemoveTopic(p as Topic));
            RefreshCommand       = new RelayCommand(_ => Refresh());
        }

        private void AddSubject()
        {
            if (string.IsNullOrWhiteSpace(NewSubjectName)) return;

            var s = new Subject
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = NewSubjectName.Trim(),
                IsEge = false,
                ScoreScaleKey = null
            };

            App.ProgressService.AddSubject(s);
            Subjects.Add(s);

            NewSubjectName = string.Empty;
            OnPropertyChanged(nameof(NewSubjectName));
        }

        private void RemoveSubject(Subject? s)
        {
            if (s == null) return;

            App.ProgressService.RemoveSubject(s.Id);
            Subjects.Remove(s);

            if (SelectedSubject == s)
                SelectedSubject = Subjects.FirstOrDefault();
        }

        private void AddTopic()
        {
            if (SelectedSubject == null) return;
            if (string.IsNullOrWhiteSpace(NewTopicName)) return;

            var t = new Topic
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = NewTopicName.Trim()
            };

            App.ProgressService.AddTopic(SelectedSubject.Id, t);
            SelectedSubject.Topics.Add(t);

            NewTopicName = string.Empty;
            OnPropertyChanged(nameof(NewTopicName));
            OnPropertyChanged(nameof(SelectedTopicList));
        }

        private void RemoveTopic(Topic? t)
        {
            if (SelectedSubject == null || t == null) return;

            App.ProgressService.RemoveTopic(SelectedSubject.Id, t.Id);
            SelectedSubject.Topics.Remove(t);

            OnPropertyChanged(nameof(SelectedTopicList));
        }

        /// <summary>
        /// Обновить статистику по предметам — вызывается из кнопки «Обновить».
        /// </summary>
        public void Refresh()
        {
            foreach (var s in Subjects)
                OnPropertyChanged(nameof(s));
        }
    }
}